using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Basket.API.Data.Interfaces
{
    public interface IHttpClientDataContext
    {
        Task<HttpResponseMessage> GetPostCommentsAsync(int id);
        Task<HttpResponseMessage> DummyMethodAsync(dynamic request);
    }
}
