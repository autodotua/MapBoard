using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace MapBoard.Model
{
    public class ItemsOperationErrorCollection : List<ItemsOperationError>
    {
        public ItemsOperationError Add(string name, string message)
        {
            var item = new ItemsOperationError(name, message);
            Add(item);
            return item;
        }

        public ItemsOperationError Add(string name, Exception ex)
        {
            var item = new ItemsOperationError(name, ex);
            Add(item);
            return item;
        }
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

    public class ItemsOperationException : Exception
    {
        public ItemsOperationException(ItemsOperationErrorCollection errors)
        {
            Errors = errors;
        }
        public ItemsOperationErrorCollection Errors { get;private set; }
    }
}