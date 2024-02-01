using System;


namespace SOMIOD.Exceptions
{
    public class ModelNotFound : Exception
    {
        public ModelNotFound(string message, bool suffix = true) : base(message + (suffix ? " not found" : ""))
        {
        }
    }
}