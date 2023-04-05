namespace MapBoard.Model
{
    /// <summary>
    /// 将一个值类型包装为引用类型
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ValueTypeWrapper<T> where T : struct
    {
        public ValueTypeWrapper()
        {
        }

        public ValueTypeWrapper(T value)
        {
            Value = value;
        }

        public T Value { get; set; }
    }
}