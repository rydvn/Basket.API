using System.Collections.Generic;
using System.Linq;

namespace Basket.API.Models.Responses
{
    public class BaseServiceResponse<TData>
    {
        public BaseServiceResponse()
        {
            Errors = new List<string>();
        }

        public bool HasError => Errors.Any();

        public List<string> Errors { get; set; }

        public TData Data { get; set; }
    }
}
