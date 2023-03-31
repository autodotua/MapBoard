using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace MapBoard.Model
{
    /// <summary>
    /// 错误集合
    /// </summary>
    public class ItemsOperationErrorCollection : List<ItemsOperationError>
    {
        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="name"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public ItemsOperationError Add(string name, string message)
        {
            var item = new ItemsOperationError(name, message);
            Add(item);
            return item;
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        public ItemsOperationError Add(string name, Exception ex)
        {
            var item = new ItemsOperationError(name, ex);
            Add(item);
            return item;
        }
    }

    /// <summary>
    /// 项目操作错误
    /// </summary>
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

    /// <summary>
    /// 项目操作异常
    /// </summary>
    public class ItemsOperationException : Exception
    {
        public ItemsOperationException(ItemsOperationErrorCollection errors)
        {
            Errors = errors;
        }
        public ItemsOperationErrorCollection Errors { get;private set; }
    }
}