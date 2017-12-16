namespace KiraNet.AspectFlare
{
    public class CalledInterceptContext
    {
        public object Owner { get; set; }
        public string InterceptedName { get; set; }
        public object ReturnValue { get; set; }
        public bool HasResult { get; set; }
        public object Result { get; set; }
    }
}
