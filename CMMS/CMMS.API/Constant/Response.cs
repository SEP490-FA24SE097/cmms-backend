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

    public class TaxCodeCheckApiResponse
    {
        public string Code { get; set; } 
        public string Desc { get; set; } 
        public DataObject? Data { get; set; } 
    }

    public class DataObject
    {
        public string Id { get; set; }
        public string Name { get; set; } 
        public string InternationalName { get; set; } 
        public string ShortName { get; set; } 
        public string Address { get; set; } 
    }
}
