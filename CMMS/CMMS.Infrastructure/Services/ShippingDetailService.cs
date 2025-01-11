using Azure.Core;
using CMMS.Core.Constant;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Services
{
    public interface IShippingDetailService
    {
        #region CURD
        Task<ShippingDetail> FindAsync(string id);
        IQueryable<ShippingDetail> GetAll();
        IQueryable<ShippingDetail> Get(Expression<Func<ShippingDetail, bool>> where);
        IQueryable<ShippingDetail> Get(Expression<Func<ShippingDetail, bool>> where, params Expression<Func<ShippingDetail, object>>[] includes);
        IQueryable<ShippingDetail> Get(Expression<Func<ShippingDetail, bool>> where, Func<IQueryable<ShippingDetail>, IIncludableQueryable<ShippingDetail, object>> include = null);
        Task AddAsync(ShippingDetail ShippingDetail);
        Task AddRange(IEnumerable<ShippingDetail> ShippingDetails);
        void Update(ShippingDetail ShippingDetail);
        Task<bool> Remove(string id);
        Task<bool> CheckExist(Expression<Func<ShippingDetail, bool>> where);
        Task<bool> SaveChangeAsync();
        #endregion

        public Task<Message> SendRequestToCancleShipping(SendRequestShippingDetail request);
        public Task<Message> ProcessRequestFromShipper(ProcessRequestShippingDetailFromShipper processRequest);
    }
    public class ShippingDetailService : IShippingDetailService
    {
        private IShippingDetailRepository _shippingDetailRepository;
        private IUnitOfWork _unitOfWork;
        private IUserRepository _userRepository;

        public ShippingDetailService(IShippingDetailRepository shippingDetailRepository,
            IUnitOfWork unitOfWork, IUserRepository userRepository)
        {
            _shippingDetailRepository = shippingDetailRepository;
            _unitOfWork = unitOfWork;
            _userRepository = userRepository;
        }

        #region CURD 
        public async Task AddAsync(ShippingDetail ShippingDetail)
        {
            await _shippingDetailRepository.AddAsync(ShippingDetail);
        }

        public async Task AddRange(IEnumerable<ShippingDetail> ShippingDetails)
        {
            await _shippingDetailRepository.AddRangce(ShippingDetails);
        }

        public Task<bool> CheckExist(Expression<Func<ShippingDetail, bool>> where)
        {
            return _shippingDetailRepository.CheckExist(where);
        }

        public Task<ShippingDetail> FindAsync(string id)
        {
            return _shippingDetailRepository.FindAsync(id);
        }

        public IQueryable<ShippingDetail> Get(Expression<Func<ShippingDetail, bool>> where)
        {
            return _shippingDetailRepository.Get(where);
        }

        public IQueryable<ShippingDetail> Get(Expression<Func<ShippingDetail, bool>> where, params Expression<Func<ShippingDetail, object>>[] includes)
        {
            return _shippingDetailRepository.Get(where, includes);
        }

        public IQueryable<ShippingDetail> Get(Expression<Func<ShippingDetail, bool>> where, Func<IQueryable<ShippingDetail>, IIncludableQueryable<ShippingDetail, object>> include = null)
        {
            return _shippingDetailRepository.Get(where, include);
        }

        public IQueryable<ShippingDetail> GetAll()
        {
            return _shippingDetailRepository.GetAll();
        }

        public async Task<bool> Remove(string id)
        {
            return await _shippingDetailRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }


        public void Update(ShippingDetail ShippingDetail)
        {
            _shippingDetailRepository.Update(ShippingDetail);
        }
        #endregion



        public async Task<Message> ProcessRequestFromShipper(ProcessRequestShippingDetailFromShipper processRequest)
        {
            var message = new Message();
            var shippingDetail = await _shippingDetailRepository.FindAsync(processRequest.ShippingDetailCode);
            if (shippingDetail != null)
            {
                var shipper = await _userRepository.FindAsync(processRequest.ShipperId);
                if (processRequest.StoreStaffId.Equals(shipper.StoreId))
                {
                    switch (processRequest.ShipperDetailStatus)
                    {
                        case (int)ShippingDetailStatus.Approved:
                            {
                                shippingDetail.ShippingDetailStatus = (int)ShippingDetailStatus.Pending;
                                shippingDetail.ShipperId = processRequest.ShipperId;
                                _shippingDetailRepository.Update(shippingDetail);
                                var result = await _unitOfWork.SaveChangeAsync();
                                if (result)
                                {
                                    message.StatusCode = 200;
                                    message.Content = $"Chuyển đổi người giao hàng cho hóa đơn {shippingDetail.InvoiceId} thành công";
                                }
                                break;
                            }
                        case (int)ShippingDetailStatus.Rejected:
                            {
                                shippingDetail.ShippingDetailStatus = (int)ShippingDetailStatus.Rejected;
                                _shippingDetailRepository.Update(shippingDetail);
                                var result = await _unitOfWork.SaveChangeAsync();
                                if (result)
                                {
                                    message.StatusCode = 200;
                                    message.Content = $"Từ chối yêu cầu chuyển đổi đơn hàng.";
                                }
                                break;
                            }
                    }
                    return message;
                }
                message.StatusCode = 404;
                message.Content = "Chỉ được gán cho shipper trực thuộc cửa hàng mà bạn đang quản lí";

            }
            return message;
        }

        public async Task<Message> SendRequestToCancleShipping(SendRequestShippingDetail request)
        {
            var message = new Message();
            var shippingDetail = await _shippingDetailRepository.FindAsync(request.ShippingDetailCode);
            if (shippingDetail != null)
            {
                shippingDetail.ShippingDetailStatus = (int)ShippingDetailStatus.RequestToChange;
                shippingDetail.Reason = request.Reason;
                _shippingDetailRepository.Update(shippingDetail);
                var result = await _unitOfWork.SaveChangeAsync();
                if (result)
                {
                    message.StatusCode = 200;
                    message.Content = $"Gửi yêu cầu chuyển đổi đơn hàng của hóa đơn {shippingDetail.InvoiceId} thành công";
                }
                else
                {
                    message.StatusCode = 500;
                }
            }
            return message;
        }

    }
}
