using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace MapBoard.Model
{
    public class ItemsOperationErrorCollection : List<ItemsOperationError>
    {
    }

    public class ItemsOperationError
    {
        public ItemsOperationError(string name, string message)
        {
            Name = name;
            ErrorMessage = message;
        }

        public ItemsOperationError(string name, Exception ex)
        {
            Name = name;
            Exception = ex;
            ErrorMessage = ex.Message;
        }

        public string Name { get; set; }
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }
    }
}