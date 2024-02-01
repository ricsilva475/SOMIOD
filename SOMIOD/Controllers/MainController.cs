using SOMIOD.Exceptions;
using SOMIOD.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;
using RouteAttribute = System.Web.Http.RouteAttribute;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Threading;


namespace SOMIOD.Controllers
{
    public class MainController : ApiController

    {
        #region Variables

        String strDataConn = SOMIOD.Properties.Settings.Default.ConnStr;

        private readonly List<string> _validEventTypes = new List<string>() { "CREATE", "DELETE", "BOTH" };

        public static string localhost = "127.0.0.1";

        #endregion

        #region Geral Methods
        private static int IsParentValid(SqlConnection db, string parentType, string parentName, string childType, string childName)
        {
            var cmd =
                new
                    SqlCommand(
                    "SELECT c.id FROM " + childType + " c JOIN " + parentType + " p ON (c.parent = p.id) WHERE p.name=@ParentName AND c.name=@ChildName",
                    db);
            cmd.Parameters.AddWithValue("@ParentName", parentName.ToLower());
            cmd.Parameters.AddWithValue("@ChildName", childName.ToLower());
            var reader = cmd.ExecuteReader();

            if (!reader.Read())
                throw new
                    ModelNotFound("Couldn't find " + childType.ToLower() + " '" + childName + "' in " + parentType.ToLower() + " '" + parentName + "'",
                                           false);

            int childId = reader.GetInt32(0);
            reader.Close();
            return childId;
        }

        private static int GetParentId(SqlConnection conn, string parentType, string parentName)
        {
            var cmd = new SqlCommand("SELECT id FROM " + parentType + " WHERE name=@ParentName", conn);
            cmd.Parameters.AddWithValue("@ParentName", parentName.ToLower());
            var reader = cmd.ExecuteReader();

            if (!reader.Read())
                throw new ModelNotFound("Couldn't find " + parentType.ToLower() + " '" + parentName + "'", false);

            int parentId = reader.GetInt32(0);
            reader.Close();
            return parentId;
        }

        public void brokerPublish(string channel, string message, string endpoint)
        {
            MqttClient m_cClient = new MqttClient(endpoint);
            string[] m_strTopicsInfo = { channel };

            m_cClient.Connect(Guid.NewGuid().ToString());

            m_cClient.Publish(channel, Encoding.UTF8.GetBytes(message));
        }

        #endregion

        #region Application

        [Route("api/somiod")]
        public IHttpActionResult GetAllApplications()
        {
            List<SOMIOD.Models.Application> applications = new List<SOMIOD.Models.Application>();
            SqlConnection conn = null;

            try
            {
                conn = new SqlConnection(strDataConn);
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT * FROM Applications ORDER BY id", conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    SOMIOD.Models.Application p = new SOMIOD.Models.Application();
                    p.id = reader.GetInt32(0);
                    p.name = reader.GetString(1);
                    p.creation_dt = reader.GetDateTime(2);
                    //p.creation_dt = reader.GetDateTime(2).ToString("dd:MM:yyyy");

                    applications.Add(p);
                }

                // Retorna a resposta em XML
                return new XmlResult<List<SOMIOD.Models.Application>>(applications);
            }
            catch (Exception e)
            {
                if (conn.State == System.Data.ConnectionState.Open) conn.Close();
                Console.WriteLine(e.Message);
                return InternalServerError();
            }
            finally
            {
                if (conn != null) conn.Close();
            }

        }

        [Route("api/somiod/{appName}")]
        public IHttpActionResult GetApplicationByName(string appName)
        {
            if (string.IsNullOrEmpty(appName))
            {
                return BadRequest("O nome não pode ser nulo ou vazio.");
            }

            SOMIOD.Models.Application application = null;
            SqlConnection conn = null;

            try
            {
                conn = new SqlConnection(strDataConn);
                conn.Open();

                // Use um parâmetro para evitar SQL Injection
                string query = "SELECT * FROM applications WHERE name = @name";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@name", appName);

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    application = new SOMIOD.Models.Application
                    {
                        id = reader.GetInt32(0),
                        name = reader.GetString(1),
                        creation_dt = reader.GetDateTime(2)
                        // Adicione outras propriedades conforme necessário
                    };
                }
            }
            catch (Exception e)
            {
                if (conn.State == System.Data.ConnectionState.Open) conn.Close();
                Console.WriteLine(e.Message);
                return InternalServerError();
            }
            finally
            {
                if (conn != null) conn.Close();
            }

            if (application == null)
            {
                return NotFound();
            }

            return new XmlResult<SOMIOD.Models.Application>(application);
        }

        [Route("api/somiod")]
        public IHttpActionResult PostApplication()
        {
            try
            {
                // Lê os dados XML do corpo da solicitação
                var xmlRequest = Request.Content.ReadAsStringAsync().Result;

                // Deserializa os dados XML para o objeto Application
                var serializer = new XmlSerializer(typeof(SOMIOD.Models.Application));
                using (TextReader reader = new StringReader(xmlRequest))
                {
                    var application = (SOMIOD.Models.Application)serializer.Deserialize(reader);

                    // Verifica se a aplicação com o mesmo nome já existe
                    if (ApplicationExists(application.name))
                    {
                        // Retorna um status HTTP 409 (Conflict) indicando que a aplicação já existe
                        var response = new HttpResponseMessage(HttpStatusCode.Conflict)
                        {
                            Content = new StringContent("Application with the same name already exists."),
                            ReasonPhrase = "Conflict"
                        };
                        return ResponseMessage(response);
                    }

                    // Insere os dados no banco de dados
                    using (SqlConnection conn = new SqlConnection(strDataConn))
                    {
                        conn.Open();

                        // Insere a aplicação
                        SqlCommand cmd = new SqlCommand("INSERT INTO applications (name, creation_dt) VALUES (@name, @creation_dt)", conn);
                        cmd.Parameters.AddWithValue("@name", application.name);
                        cmd.Parameters.AddWithValue("@creation_dt", DateTime.Now);

                        cmd.ExecuteNonQuery();

                        conn.Close();

                        // Retorna uma mensagem indicando sucesso
                        brokerPublish("application", $"Application created... Name: {application.name}", localhost);
                        return Ok("Application created successfully.");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                // Retorna um status HTTP 500 (InternalServerError) se algo der errado durante o processamento
                return InternalServerError();
            }
        }

        private bool ApplicationExists(string appName)
        {
            // Verifica se a aplicação com o mesmo nome já existe no banco de dados
            using (SqlConnection conn = new SqlConnection(strDataConn))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM applications WHERE name = @name", conn);
                cmd.Parameters.AddWithValue("@name", appName);

                int count = (int)cmd.ExecuteScalar();

                conn.Close();

                return count > 0;
            }
        }





        [Route("api/somiod/{appName}")]
        public IHttpActionResult PutApplication(string appName)
        {
            if (string.IsNullOrEmpty(appName))
            {
                return BadRequest("O nome não pode ser nulo ou vazio.");
            }

            try
            {
                // Lê os dados XML do corpo da solicitação
                var xmlRequest = Request.Content.ReadAsStringAsync().Result;

                // Deserializa os dados XML para o objeto Application
                var serializer = new XmlSerializer(typeof(SOMIOD.Models.Application));
                using (TextReader reader = new StringReader(xmlRequest))
                {
                    SOMIOD.Models.Application updatedApplication = (SOMIOD.Models.Application)serializer.Deserialize(reader);

                    using (SqlConnection conn = new SqlConnection(strDataConn))
                    {
                        conn.Open();

                        // Use a parameterized query to avoid SQL Injection
                        string query = "UPDATE applications SET name = @newName WHERE name = @oldName";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@newName", updatedApplication.name);
                        cmd.Parameters.AddWithValue("@oldName", appName);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return Ok("Application updated successfully.");
                        }
                        else
                        {
                            return NotFound(); // Application with the specified name not found
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601 || ex.Number == 2627)
                {
                    Console.WriteLine("A aplicação com este nome já existe.");
                    return Conflict(); // Application with the new name already exists
                }
                else
                {
                    Console.WriteLine($"Erro ao atualizar registro: {ex.Message}");
                    return InternalServerError();
                }
            }
        }



        // Delete para apagar a application
        [Route("api/somiod/{appName}")]
        public IHttpActionResult DeleteApplication(string appName)
        {
            if (string.IsNullOrEmpty(appName))
            {
                return BadRequest("O nome não pode ser nulo ou vazio.");
            }

            using (SqlConnection conn = new SqlConnection(strDataConn))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // Get the application ID by name
                    int applicationId = GetApplicationIdByName(conn, appName, transaction);

                    // Check if there are associated containers
                    using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM containers WHERE parent = @id", conn, transaction))
                    {
                        checkCmd.Parameters.AddWithValue("@id", applicationId);
                        int containerCount = (int)checkCmd.ExecuteScalar();

                        if (containerCount > 0)
                        {
                            // If there are associated containers, do not delete
                            // You can throw an exception, return an error response, or handle it as needed
                            throw new InvalidOperationException("Cannot delete application with associated containers.");
                        }
                    }

                    // If no associated containers, proceed with the deletion
                    using (SqlCommand deleteCmd = new SqlCommand("DELETE FROM applications WHERE id = @id", conn, transaction))
                    {
                        deleteCmd.Parameters.AddWithValue("@id", applicationId);
                        deleteCmd.ExecuteNonQuery();
                    }

                    // Commit the transaction if everything is successful
                    transaction.Commit();

                    return Ok("Application deleted successfully.");
                }
                catch (Exception ex)
                {
                    // Rollback the transaction in case of an exception
                    transaction.Rollback();

                    // Handle the exception (e.g., log it, display an error message)
                    Console.WriteLine($"Error deleting record: {ex.Message}");
                    return InternalServerError();
                }
                finally
                {
                    // Close the connection
                    conn.Close();
                }
            }
        }

        private static int GetApplicationIdByName(SqlConnection conn, string appName, SqlTransaction transaction)
        {
            var cmd = new SqlCommand("SELECT id FROM applications WHERE name=@Name", conn, transaction);
            cmd.Parameters.AddWithValue("@Name", appName.ToLower());
            var reader = cmd.ExecuteReader();

            if (!reader.Read())
                throw new ModelNotFound("Couldn't find application with name '" + appName + "'", false);

            int applicationId = reader.GetInt32(0);
            reader.Close();
            return applicationId;
        }

        #endregion

        #region Container
        // GET: Container
        [Route("api/somiod/{appName}/container")]
        public IHttpActionResult GetAllContainers(string appName)
        {
            List<Container> containers = new List<Container>();
            SqlConnection conn = null;

            try
            {
                conn = new SqlConnection(strDataConn);
                conn.Open();

                var cmd = new SqlCommand("SELECT * FROM containers c JOIN applications a ON (c.parent = a.id) WHERE a.name=@application", conn);
                cmd.Parameters.AddWithValue("@application", appName.ToLower());
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Container c = new Container();
                    c.id = reader.GetInt32(0);
                    c.name = reader.GetString(1);
                    c.creation_dt = reader.GetDateTime(2);
                    c.parent = reader.GetInt32(3);
                    //p.Creation_dt = reader.GetDateTime(2).ToString("dd:MM:yyyy");

                    containers.Add(c);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return InternalServerError();
            }
            finally
            {
                if (conn != null && conn.State == System.Data.ConnectionState.Open)
                    conn.Close();
            }

            // Serializar a lista de contêineres para XML
            var serializer = new XmlSerializer(typeof(List<Container>));
            StringWriter sw = new StringWriter();
            serializer.Serialize(sw, containers);
            string xmlResult = sw.ToString();

            // Retornar os valores em XML
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(xmlResult, Encoding.UTF8, "application/xml")
            };

            return ResponseMessage(response);
        }


        // get container by name

        [Route("api/somiod/{appName}/{containerName}")]
        public IHttpActionResult GetContainerByName(string appName, string containerName)
        {
            Container c = new Container();
            SqlConnection conn = null;

            try
            {
                conn = new SqlConnection(strDataConn);
                conn.Open();

                // Modifique a consulta SQL para incluir a condição do nome do container e o ID da aplicação
                SqlCommand cmd = new SqlCommand("SELECT * FROM containers c JOIN applications a ON c.parent = a.id WHERE LOWER(a.name) = LOWER(@application) AND c.name = LOWER(@container)", conn);
                cmd.Parameters.AddWithValue("@application", appName);
                cmd.Parameters.AddWithValue("@container", containerName);

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    c.id = reader.GetInt32(0);
                    c.name = reader.GetString(1);
                    c.creation_dt = reader.GetDateTime(2);
                    c.parent = reader.GetInt32(3);
                    //p.creation_dt = reader.GetDateTime(2).ToString("dd:MM:yyyy");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return InternalServerError();
            }
            finally
            {
                if (conn != null && conn.State == System.Data.ConnectionState.Open)
                    conn.Close();
            }

            // Serializar o objeto Container para XML
            var serializer = new XmlSerializer(typeof(Container));
            StringWriter sw = new StringWriter();
            serializer.Serialize(sw, c);
            string xmlResult = sw.ToString();

            // Retornar os valores em XML
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(xmlResult, Encoding.UTF8, "application/xml")
            };

            return ResponseMessage(response);
        }


        [Route("api/somiod/{appName}/container")]
        public IHttpActionResult PostContainer(string appName)
        {
            SqlConnection conn = null;
            try
            {
                conn = new SqlConnection(strDataConn);
                conn.Open();

                // Lê os dados XML do corpo da solicitação
                var xmlRequest = Request.Content.ReadAsStringAsync().Result;

                // Deserializa os dados XML para o objeto Application
                var serializer = new XmlSerializer(typeof(SOMIOD.Models.Container));
                using (TextReader reader = new StringReader(xmlRequest))
                {
                    SOMIOD.Models.Container application = (SOMIOD.Models.Container)serializer.Deserialize(reader);

                    // Obtenha o id da aplicação (parent) com base no appName
                    int parentId = GetParentId(conn, "applications", appName);

                    SqlCommand cmd = new SqlCommand("INSERT INTO containers(name, creation_dt, parent) VALUES (@name, @creation_dt, @parent)", conn);
                    cmd.Parameters.AddWithValue("@name", application.name);
                    cmd.Parameters.AddWithValue("@creation_dt", DateTime.Now);
                    cmd.Parameters.AddWithValue("@parent", parentId);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        brokerPublish("container", $"Container inserted in {appName}", localhost);
                        return Ok("Container adicionado com sucesso."); // Retorna um código HTTP 200 (OK) e a mensagem de sucesso
                    }
                    else
                    {
                        return BadRequest("Erro ao adicionar o Container."); // Retorna um código HTTP 400 (Bad Request) em caso de falha
                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                // Pode ser interessante retornar uma resposta de erro adequada, por exemplo, BadRequest
                return BadRequest("Erro ao adicionar o Container.");
            }
            finally
            {
                if (conn != null && conn.State == System.Data.ConnectionState.Open)
                    conn.Close();
            }
        }

        private static int IsContainerParentValid(SqlConnection conn, string appName, string containerName)
        {
            return IsParentValid(conn, "applications", appName, "containers", containerName);
        }


        // update container

        [Route("api/somiod/{appName}/{containerName}")]
        public IHttpActionResult PutContainer(string appName, string containerName)
        {
            SqlConnection conn = null;
            try
            {
                conn = new SqlConnection(strDataConn);
                conn.Open();

                // Verifica se o parent é válido antes de atualizar
                IsContainerParentValid(conn, appName, containerName);

                // Lê os dados XML do corpo da solicitação
                var xmlRequest = Request.Content.ReadAsStringAsync().Result;

                // Deserializa os dados XML para o objeto Container
                var serializer = new XmlSerializer(typeof(SOMIOD.Models.Container));
                using (TextReader reader = new StringReader(xmlRequest))
                {
                    SOMIOD.Models.Container newContainer = (SOMIOD.Models.Container)serializer.Deserialize(reader);

                    SqlCommand cmd = new SqlCommand("UPDATE containers SET name=@NewName WHERE name=@Name", conn);
                    cmd.Parameters.AddWithValue("@Name", containerName);
                    cmd.Parameters.AddWithValue("@NewName", newContainer.name);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return Ok("Container atualizado com sucesso."); // Retorna um código HTTP 200 (OK) e a mensagem de sucesso
                    }
                    else
                    {
                        return NotFound(); // Retorna um código HTTP 404 (Not Found) se o contêiner não for encontrado
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                // Pode ser interessante retornar uma resposta de erro adequada, por exemplo, BadRequest
                return BadRequest("Erro ao atualizar o Container.");
            }
            finally
            {
                if (conn != null && conn.State == System.Data.ConnectionState.Open)
                    conn.Close();
            }
        }




        // delete container
        [Route("api/somiod/{appName}/{containerName}/containerid")]
        public int GetContainerIdByName(string appName, string containerName)
        {
            SqlConnection conn = null;
            try
            {
                conn = new SqlConnection(strDataConn);
                conn.Open();

                // Obter o ID da aplicação usando o nome da aplicação
                int applicationId = GetParentId(conn, "applications", appName);

                // Obter o ID do container usando o nome do container e o ID da aplicação
                SqlCommand cmd = new SqlCommand("SELECT id FROM containers WHERE LOWER(name) = LOWER(@containerName) AND parent = @applicationId", conn);
                cmd.Parameters.AddWithValue("@containerName", containerName);
                cmd.Parameters.AddWithValue("@applicationId", applicationId);

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return reader.GetInt32(0);
                }
                else
                {
                    // Handle the case where the container was not found
                    // You might want to throw an exception or return a specific value
                    Console.WriteLine("Container not found.");
                    return -1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
            finally
            {
                if (conn != null && conn.State == System.Data.ConnectionState.Open)
                    conn.Close();
            }
        }


        [Route("api/somiod/{appName}/{containerName}")]
        public IHttpActionResult DeleteContainer(string appName, string containerName)
        {
            SqlConnection conn = null;
            try
            {
                conn = new SqlConnection(strDataConn);
                conn.Open();

                // Verifica se o parent é válido antes de excluir
                IsContainerParentValid(conn, appName, containerName);

                // Check if there are associated data records
                int id = GetContainerIdByName(appName, containerName);
                using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM data WHERE parent = @id", conn))
                {
                    checkCmd.Parameters.AddWithValue("@id", id);
                    int dataCount = (int)checkCmd.ExecuteScalar();

                    if (dataCount > 0)
                    {
                        // If there are associated data records, return an error response
                        return BadRequest("Não é possível excluir o Container com dados associados.");
                    }
                }

                // If no associated data records, proceed with the deletion
                using (SqlCommand cmd = new SqlCommand("DELETE FROM containers WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return Ok("Container excluído com sucesso."); // Retorna um código HTTP 200 (OK) e a mensagem de sucesso
                    }
                    else
                    {
                        return NotFound(); // Retorna um código HTTP 404 (Not Found) se o contêiner não for encontrado
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                // Pode ser interessante retornar uma resposta de erro adequada, por exemplo, InternalServerError
                return InternalServerError();
            }
            finally
            {
                if (conn != null && conn.State == System.Data.ConnectionState.Open)
                    conn.Close();
            }
        }

        #endregion

        #region Data
        [Route("api/somiod/{appName}/{containerName}/data")]
        public IHttpActionResult GetAllData(string appName, string containerName)
        {
            List<Data> data = new List<Data>();
            SqlConnection conn = null;

            try
            {
                conn = new SqlConnection(strDataConn);
                conn.Open();

                // Get the parent IDs for the application and container
                int applicationId = GetParentId(conn, "applications", appName);
                int containerId = GetParentId(conn, "containers", containerName);

                // Use the obtained parent IDs in the SQL query
                SqlCommand cmd = new SqlCommand("SELECT * FROM data WHERE parent = @containerId", conn);
                cmd.Parameters.AddWithValue("@containerId", containerId);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Data d = new Data();
                    d.id = reader.GetInt32(0);
                    d.content = reader.GetString(1);
                    d.creation_dt = reader.GetDateTime(2);
                    d.parent = reader.GetInt32(3);

                    data.Add(d);
                }

                // Retorna os dados em formato XML usando a classe XmlResult
                return new XmlResult<List<Data>>(data);
            }
            catch (Exception e)
            {
                // Handle the exception appropriately, such as logging or rethrowing
                Debug.WriteLine(e.Message);
                // Retorna uma resposta de erro
                return InternalServerError();
            }
            finally
            {
                if (conn != null) conn.Close();
            }
        }


        [Route("api/somiod/{appName}/{containerName}/data")]
        public IHttpActionResult PostData(string appName, string containerName)
        {
            SqlConnection conn = null;

            try
            {
                conn = new SqlConnection(strDataConn);
                conn.Open();

                // Retrieve the parent ID for the specified containerName
                int containerId = GetParentId(conn, "containers", containerName);

                if (containerId == -1)
                {
                    return BadRequest("Container not found");
                }

                // Read the XML content from the request body
                var xmlRequest = Request.Content.ReadAsStringAsync().Result;

                // Deserialize the XML data into the Data object
                var serializer = new XmlSerializer(typeof(Data));
                using (TextReader reader = new StringReader(xmlRequest))
                {
                    Data newData = (Data)serializer.Deserialize(reader);

                    // Insert the data into the database with the retrieved containerId as parent
                    SqlCommand cmd = new SqlCommand("INSERT INTO data (content, creation_dt, parent) VALUES (@content, @creation_dt, @parent)", conn);
                    cmd.Parameters.AddWithValue("@content", newData.content);
                    cmd.Parameters.AddWithValue("@creation_dt", DateTime.Now);
                    cmd.Parameters.AddWithValue("@parent", containerId);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // Publish a message to the MQTT broker
                        brokerPublish("data", $"{newData.content}", localhost);

                        return Ok("Data inserted successfully");
                    }
                    else
                    {
                        return BadRequest("Failed to insert data");
                    }
                }
            }
            catch (ModelNotFound e)
            {
                // Handle the model not found exception appropriately
                Console.WriteLine(e.Message);
                return BadRequest("Model not found: " + e.Message);
            }
            catch (Exception e)
            {
                // Handle other exceptions appropriately, such as logging or rethrowing
                Console.WriteLine(e.ToString());
                return BadRequest("Failed to insert data");
            }
            finally
            {
                if (conn != null) conn.Close();
            }
        }

        [Route("api/somiod/{appName}/{containerName}/{dataId}")]
        public IHttpActionResult DeleteData(string appName, string containerName, int dataId)
        {
            SqlConnection conn = null;

            try
            {
                conn = new SqlConnection(strDataConn);
                conn.Open();

                // Check if there are associated subscriptions
                SqlCommand checkSubscriptionsCmd = new SqlCommand("SELECT COUNT(*) FROM subscriptions WHERE parent = @dataId", conn);
                checkSubscriptionsCmd.Parameters.AddWithValue("@dataId", dataId);
                int subscriptionCount = (int)checkSubscriptionsCmd.ExecuteScalar();

                if (subscriptionCount > 0)
                {
                    // Delete associated subscriptions
                    SqlCommand deleteSubscriptionsCmd = new SqlCommand("DELETE FROM subscriptions WHERE parent = @dataId", conn);
                    deleteSubscriptionsCmd.Parameters.AddWithValue("@dataId", dataId);
                    int rowsAffectedSubscriptions = deleteSubscriptionsCmd.ExecuteNonQuery();

                    if (rowsAffectedSubscriptions <= 0)
                    {
                        return BadRequest("Failed to delete associated subscriptions");
                    }
                }

                // Proceed with data deletion
                int applicationId = GetParentId(conn, "applications", appName);
                int containerId = GetParentId(conn, "containers", containerName);

                SqlCommand deleteDataCmd = new SqlCommand("DELETE FROM data WHERE id = @dataId AND parent = @containerId", conn);
                deleteDataCmd.Parameters.AddWithValue("@dataId", dataId);
                deleteDataCmd.Parameters.AddWithValue("@containerId", containerId);

                int rowsAffectedData = deleteDataCmd.ExecuteNonQuery();

                if (rowsAffectedData > 0)
                {
                    return Ok("Data deleted successfully");
                }
                else
                {
                    return BadRequest("Failed to delete data");
                }
            }
            catch (Exception e)
            {
                // Handle the exception appropriately, such as logging or rethrowing
                Console.WriteLine(e.ToString());
                return InternalServerError();
            }
            finally
            {
                if (conn != null) conn.Close();
            }
        }

        #endregion

        #region Subscription
        [Route("api/somiod/{appName}/{containerName}/subscription")]
        public IHttpActionResult PostSubscription(string appName, string containerName)
        {
            SqlConnection conn = null;

            try
            {
                conn = new SqlConnection(strDataConn);
                conn.Open();

                // Retrieve the parent ID for the specified containerName
                int containerId = GetParentId(conn, "containers", containerName);

                if (containerId == -1)
                {
                    return BadRequest("Container not found");
                }

                // Read the XML content from the request body
                var xmlContent = Request.Content.ReadAsStringAsync().Result;

                // Deserialize the XML content into Subscription object
                var serializer = new XmlSerializer(typeof(Subscription));
                using (TextReader reader = new StringReader(xmlContent))
                {
                    Subscription newSubscription = (Subscription)serializer.Deserialize(reader);

                    string eventType = newSubscription.subscription_event.Trim().ToUpper();
                    if (!_validEventTypes.Contains(eventType))
                    {
                        return BadRequest("Invalid subscription_event. Allowed values are: CREATE, DELETE, BOTH");
                    }

                    // Insert the subscription into the database
                    SqlCommand cmd = new SqlCommand("INSERT INTO Subscriptions (Name, Parent, Subscription_Event, Endpoint, Creation_dt) VALUES (@name, @parent, @subscription_event, @endpoint, @creation_dt)", conn);
                    cmd.Parameters.AddWithValue("@name", newSubscription.name.ToLower());
                    cmd.Parameters.AddWithValue("@creation_dt", DateTime.Now);
                    cmd.Parameters.AddWithValue("@parent", containerId);
                    cmd.Parameters.AddWithValue("@subscription_event", newSubscription.subscription_event.ToUpper()); // Ensure uppercase for consistency
                    cmd.Parameters.AddWithValue("@endpoint", newSubscription.endpoint);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // Publish a message to the MQTT broker
                        brokerPublish("subscription", $"Subscription inserted for {containerName} in {appName}", localhost);

                        return Ok("Subscription inserted successfully");
                    }
                    else
                    {
                        return BadRequest("Failed to insert subscription");
                    }
                }
            }
            catch (ModelNotFound e)
            {
                // Handle the model not found exception appropriately
                Console.WriteLine(e.Message);
                return BadRequest("Model not found: " + e.Message);
            }
            catch (Exception e)
            {
                // Handle other exceptions appropriately, such as logging or rethrowing
                Console.WriteLine(e.ToString());
                return BadRequest("Failed to insert subscription");
            }
            finally
            {
                if (conn != null) conn.Close();
            }
        }

        //get all subscriptions
        [Route("api/somiod/{appName}/{containerName}/subscription")]
        public IHttpActionResult GetAllSubscriptions(string appName, string containerName)
        {
            List<Subscription> subscriptions = new List<Subscription>();
            SqlConnection conn = null;

            try
            {
                conn = new SqlConnection(strDataConn);
                conn.Open();

                // Get the parent IDs for the application and container
                int applicationId = GetParentId(conn, "applications", appName);
                int containerId = GetParentId(conn, "containers", containerName);

                // Use the obtained parent IDs in the SQL query
                SqlCommand cmd = new SqlCommand("SELECT * FROM subscriptions WHERE parent = @containerId", conn);
                cmd.Parameters.AddWithValue("@containerId", containerId);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Subscription s = new Subscription();
                    s.id = reader.GetInt32(0);
                    s.name = reader.GetString(1);
                    s.creation_dt = reader.GetDateTime(2);
                    s.parent = reader.GetInt32(3);
                    s.subscription_event = reader.GetString(4);
                    s.endpoint = reader.GetString(5);

                    subscriptions.Add(s);
                }

                // Retorna a resposta em XML
                return new XmlResult<List<Subscription>>(subscriptions);
            }
            catch (Exception e)
            {
                // Handle the exception appropriately, such as logging or rethrowing
                Console.WriteLine(e.ToString());
                return InternalServerError();
            }
            finally
            {
                if (conn != null) conn.Close();
            }
        }

        //get subscription by name
        [Route("api/somiod/{appName}/{containerName}/subscription/{subscriptionName}")]
        public IHttpActionResult GetSubscriptionByName(string appName, string containerName, string subscriptionName)
        {
            SqlConnection conn = null;

            try
            {
                conn = new SqlConnection(strDataConn);
                conn.Open();

                // Get the parent IDs for the application and container
                int applicationId = GetParentId(conn, "applications", appName);
                int containerId = GetParentId(conn, "containers", containerName);

                // Use the obtained parent IDs in the SQL query
                SqlCommand cmd = new SqlCommand("SELECT * FROM subscriptions WHERE parent = @containerId AND name = @subscriptionName", conn);
                cmd.Parameters.AddWithValue("@containerId", containerId);
                cmd.Parameters.AddWithValue("@subscriptionName", subscriptionName.ToLower());

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    Subscription s = new Subscription();
                    s.id = reader.GetInt32(0);
                    s.name = reader.GetString(1);
                    s.creation_dt = reader.GetDateTime(2);
                    s.parent = reader.GetInt32(3);
                    s.subscription_event = reader.GetString(4);
                    s.endpoint = reader.GetString(5);

                    return new XmlResult<Subscription>(s);
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception e)
            {
                // Handle the exception appropriately, such as logging or rethrowing
                Console.WriteLine(e.ToString());
                return InternalServerError();
            }
            finally
            {
                if (conn != null) conn.Close();
            }
        }

        // delete subscription
        [Route("api/somiod/{appName}/{containerName}/subscription/{subscriptionId}")]
        public IHttpActionResult DeleteSubscription(string appName, string containerName, int subscriptionId)
        {
            using (SqlConnection conn = new SqlConnection(strDataConn))
            {
                try
                {
                    conn.Open();

                    var cmd = new SqlCommand("DELETE FROM Subscriptions WHERE Id = @SubscriptionId", conn);
                    cmd.Parameters.AddWithValue("@SubscriptionId", subscriptionId);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return Ok("Subscription deleted successfully");
                    }
                    else
                    {
                        return BadRequest("Failed to delete subscription");
                    }
                }
                catch (Exception e)
                {
                    // Handle the exception appropriately, such as logging or rethrowing
                    Console.WriteLine(e.ToString());
                    return InternalServerError();
                }
            }
        }

    }
    #endregion

}