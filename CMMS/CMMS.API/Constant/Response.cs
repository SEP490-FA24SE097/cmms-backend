namespace CMMS.API.Constant
{
    public class Response
    {
        public object data { get; set; }    
        public object meta { get; set; }
        public object links { get; set; }   
    }


    public class ResponseError
    {
        public ErrorDTO error { get; set; }
    }

    public class ErrorDTO
    {
        public string code { get; set; }
        public string message { get; set; } 
    }
}
