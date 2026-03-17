using BE_2722026_NetCoreAPI.Filter;
using DataAccess.NetCore.DbContext;
using DataAccess.NetCore.DO;
using DataAccess.NetCore.IRepository;
using DataAccess.NetCore.IServices;
using DataAccess.NetCore.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Text;

namespace BE_2722026_NetCoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomController : ControllerBase
    {
        private IRoomRepository _roomServices;
        private IRoomGenericRepository _roomGenericServices;
        private readonly IDistributedCache _cache;
        //private IUnitOfWork _unitOfWork;
        private readonly IRoomRepositoryDapper _roomRepositoryDapper;
        public RoomController(IRoomRepository roomServices, IRoomGenericRepository roomGenericServices/*,IUnitOfWork unitOfWork*/,IDistributedCache cache, IRoomRepositoryDapper roomRepositoryDapper)
        {
            _roomServices = roomServices;
            _roomGenericServices = roomGenericServices;
            //_unitOfWork = unitOfWork;
            _cache = cache;
            _roomRepositoryDapper = roomRepositoryDapper;
        }

        [BE_143026Authorize("Room_GetAll","VIEW")]
        [HttpPost("GetAll")]
        public async Task<IActionResult> GetAll(RoomGetAllRequestData requestData)
        {
            var list = new List<Rooms>();
            try
            {
                list = await _roomServices.Room_GetAll(requestData);
                //list = await _roomGenericServices.GetAll();
            }
            catch (Exception ex)
            {

            }

            return Ok(list);
        }

        
        [HttpPost("Room_GetAll_Dapper")]
        public async Task<IActionResult> Room_GetAll_Dapper(RoomGetAllRequestData requestData)
        {
            var list = new List<Rooms>();
            try
            {
                var keyCache = "Room_GetAll";
                byte[] dataCache = await _cache.GetAsync(keyCache);

                if(dataCache != null)
                {
                    //nếu trong caching có data thì gọi caching để lấy dữ liệu và trả về web
                    var cachedDataString = Encoding.UTF8.GetString(dataCache);
                    //chuyển đổi dữ liệu từ dạng string sang list object
                    list = JsonConvert.DeserializeObject<List<Rooms>>(cachedDataString);
                    return Ok(list);
                }
                else
                {
                    //nếu trong caching chưa có thì gọi database để lấy dữ liệu
                    list = await _roomRepositoryDapper.Room_GetAll(requestData);

                    //Lưu dữ liệu vào caching để lần sau gọi sẽ lấy dữ liệu từ caching
                    var cachedDataString = JsonConvert.SerializeObject(list);
                    var dataToCache = Encoding.UTF8.GetBytes(cachedDataString);

                    DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(DateTime.Now.AddMinutes(5))
                        .SetSlidingExpiration(TimeSpan.FromMinutes(3));

                    await _cache.SetAsync(keyCache, dataToCache, options);
                }
            }
            catch (Exception ex)
            {

                throw;
            }
            return list != null ? Ok(list) : NotFound();
        }

        [HttpPost("Room_Insert")]
        public async Task<IActionResult> Room_Insert(Room_InsertRequestData requestData)
        {
            try
            {
                var returnData = await _roomServices.Room_Insert(requestData);

                //var request_hotel = new Hotels
                //{
                //    CreateDate = DateTime.Now,
                //    HotelsName = "Hotel test",
                //    Description = "abc"
                //};
                //var request_room = new Rooms
                //{
                //    HotelID = requestData.HotelID,
                //    RoomNumber = requestData.RoomNumber,
                //    RoomSquare = requestData.RoomSquare,
                //    IsActive = requestData.IsActive,
                //};
                //await _unitOfWork.roomGenericRepository.Insert(request_room);

                //await _unitOfWork.hotelGenericRepository.Insert(request_hotel);

                //var returnData =  _unitOfWork.SaveChanges();

                return Ok(returnData);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("Room_Insert_Dapper")]
        public async Task<IActionResult> Room_Insert_Dapper(Room_InsertRequestData requestData)
        {
            try
            {
                //var returnData = await _roomServices.Room_Insert(requestData);

                //var request_hotel = new Hotels
                //{
                //    CreateDate = DateTime.Now,
                //    HotelsName = "Hotel test",
                //    Description = "abc"
                //};
                //var request_room = new Rooms
                //{
                //    HotelID = requestData.HotelID,
                //    RoomNumber = requestData.RoomNumber,
                //    RoomSquare = requestData.RoomSquare,
                //    IsActive = requestData.IsActive,
                //};
                //await _unitOfWork.roomGenericRepository.Insert(request_room);

                //await _unitOfWork.hotelGenericRepository.Insert(request_hotel);

                //var returnData =  _unitOfWork.SaveChanges();

                var returnData = await _roomRepositoryDapper.Room_Insert(requestData);
                
                return Ok(returnData);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
