using BE_2722026_NetCoreAPI.Filter;
using DataAccess.NetCore.DbContext;
using DataAccess.NetCore.DO;
using DataAccess.NetCore.IRepository;
using DataAccess.NetCore.IServices;
using DataAccess.NetCore.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BE_2722026_NetCoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomController : ControllerBase
    {
        private IRoomRepository _roomServices;
        private IRoomGenericRepository _roomGenericServices;
        //private IUnitOfWork _unitOfWork;
        public RoomController(IRoomRepository roomServices, IRoomGenericRepository roomGenericServices/*,IUnitOfWork unitOfWork*/)
        {
            _roomServices = roomServices;
            _roomGenericServices = roomGenericServices;
            //_unitOfWork = unitOfWork;
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
    }
}
