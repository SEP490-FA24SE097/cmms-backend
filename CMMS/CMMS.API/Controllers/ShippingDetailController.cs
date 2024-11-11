using AutoMapper;
using CMMS.API.Constant;
using CMMS.API.Helpers;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers
{
    [Route("api/shippingDetails")]
    [ApiController]
    [AllowAnonymous]
    public class ShippingDetailController : ControllerBase
    {
        private IMapper _mapper;
        private IShippingDetailService _shippingDetailService;

        public ShippingDetailController(IShippingDetailService shippingDetailService, IMapper mapper )
        {
            _mapper = mapper;
            _shippingDetailService = shippingDetailService;
        }
        [HttpGet]
        public IActionResult Get(ShippingDetailFilterModel filterModel) {
            var fitlerList = _shippingDetailService
                .Get(_ =>
                (!filterModel.FromDate.HasValue || _.ShippingDate >= filterModel.FromDate) &&
                (!filterModel.ToDate.HasValue || _.ShippingDate <= filterModel.ToDate), _ => _.Invoice);
            var total = fitlerList.Count();
            var filterListPaged = fitlerList.ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage)
                .Sort(filterModel.defaultSearch.sortBy, filterModel.defaultSearch.isAscending);
            var result = _mapper.Map<List<CustomerBalanceVM>>(filterModel);
            return Ok(new
            {
                data = result,
                pagination = new
                {
                    total,
                    perPage = filterModel.defaultSearch.perPage,
                    currentPage = filterModel.defaultSearch.currentPage,
                }
            });
        }

        //[HttpPut]
        //public IActionResult UpdateShippingDetail ()
        //{

        //}

    }
}
