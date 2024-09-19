using CMMS.Core.Entities;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Repositories;

namespace CMMS.Infrastructure.Services
{
    public interface IImageService
    {
        Task<Image> FindAsync(Guid id);
        IQueryable<Image> Get(Expression<Func<Image, bool>> where);
        Task AddRange(IEnumerable<Image> images);
        Task<bool> Remove(Guid id);
        Task<bool> SaveChangeAsync();
    }

    public class ImageService : IImageService
    {
        private IImageRepository _imageRepository;
        private IUnitOfWork _unitOfWork;
        public ImageService(IImageRepository imageRepository,IUnitOfWork unitOfWork)
        {
            _imageRepository = imageRepository;
            _unitOfWork = unitOfWork;
        }

        public Task<Image> FindAsync(Guid id)
        {
          return  _imageRepository.FindAsync(id);
        }

        public IQueryable<Image> Get(Expression<Func<Image, bool>> where)
        {
          return  _imageRepository.Get(where);
        }

        public Task AddRange(IEnumerable<Image> images)
        {
            return _imageRepository.AddRangce(images);
        }

        public Task<bool> Remove(Guid id)
        {
            return _imageRepository.Remove(id);
        }

        public Task<bool> SaveChangeAsync()
        {
            return _unitOfWork.SaveChangeAsync();
        }
    }
}
