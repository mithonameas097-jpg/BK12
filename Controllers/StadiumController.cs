using _bk12.Data;
using _bk12.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Humanizer;

namespace _bk12.Controllers
{
    [Authorize]
    public class StadiumController : Controller
    {
        private readonly Connection _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public StadiumController(Connection db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,ClubOwner")]
        public async Task<IActionResult> StadiumEdit(int id)
        {
            var userIdString = _userManager.GetUserId(User);
            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                return Forbid();
            }

            var table = (DataTable)await _db.ExecuteDB("GET_STADIUM_BY_ID", new SqlParameter[] {
                new SqlParameter("@Stadium_ID", id)
            }, returnTable: true);

            if (table == null || table.Rows.Count == 0)
            {
                return NotFound();
            }

            var row = table.Rows[0];

            if (!User.IsInRole("Admin"))
            {
                Guid ownerId = Guid.Parse(row["User_ID"].ToString()!);
                if (ownerId != userId)
                {
                    return Forbid();
                }
            }

            var model = new StadiumRequestViewModel
            {
                StadiumName = row["Stadium_Name"]?.ToString(),
                Stadium_IMG = row["Stadium_IMG"]?.ToString(),
                Map_Url = row["Map_Url"]?.ToString(),
                Description = row["Description"]?.ToString(),
                Capacity = row["Capacity"] != DBNull.Value ? Convert.ToInt32(row["Capacity"]) : 0,
                ZoneA = row["ZoneA"] != DBNull.Value ? Convert.ToInt32(row["ZoneA"]) : 0,
                ZoneB = row["ZoneB"] != DBNull.Value ? Convert.ToInt32(row["ZoneB"]) : 0,
                Stadium_Location = row["Stadium_Location"]?.ToString(),
            };

            ViewBag.StadiumId = id;
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,ClubOwner")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StadiumEdit(int id, StadiumRequestViewModel model, IFormFile? stadiumFile, [FromServices] IWebHostEnvironment env)
        {
            var userIdString = _userManager.GetUserId(User);
            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                return Forbid();
            }

            if ((model.ZoneA + model.ZoneB) > model.Capacity)
            {
                ModelState.AddModelError(string.Empty, "The combined capacity of Zone A and Zone B cannot exceed the Total Stadium Capacity.");
            }

            string finalImagePath = model.Stadium_IMG ?? "/images/default-stadium.png";
            if (stadiumFile != null && stadiumFile.Length > 0)
            {
                var webRootPath = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadsFolder = Path.Combine(webRootPath, "uploads", "stadiums");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(stadiumFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await stadiumFile.CopyToAsync(fileStream);
                }
                finalImagePath = "/uploads/stadiums/" + uniqueFileName;
            }

            if (!ModelState.IsValid)
            {
                ViewBag.StadiumId = id;
                return View(model);
            }

            var currentTime = DateTime.Now;

            if (User.IsInRole("Admin"))
            {
                SqlParameter[] adminEditParams = new SqlParameter[]
                {
                    new SqlParameter("@Stadium_ID", id),
                    new SqlParameter("@Stadium_Name", model.StadiumName),
                    new SqlParameter("@Stadium_IMG", finalImagePath),
                    new SqlParameter("@Map_Url", (object?)model.Map_Url ?? DBNull.Value),
                    new SqlParameter("@Description", (object?)model.Description ?? DBNull.Value),
                    new SqlParameter("@Updated_UID", userId),
                    new SqlParameter("@Updated_Date", currentTime),
                    new SqlParameter("@Capacity", model.Capacity),
                    new SqlParameter("@ZoneA", model.ZoneA),
                    new SqlParameter("@ZoneB", model.ZoneB),
                    new SqlParameter("@Stadium_Location", model.Stadium_Location)
                };

                await _db.ExecuteDB("ADMIN_EDIT_STADIUM", adminEditParams);

                TempData["Success"] = "Stadium updated successfully!";
                return RedirectToAction(nameof(StadiumManage));
            }

            SqlParameter[] requestParameters = new SqlParameter[]
            {
                new SqlParameter("@Stadium_ID", id),
                new SqlParameter("@User_ID", userId),
                new SqlParameter("@Request_Type", "EditStadium"),
                new SqlParameter("@Stadium_Name", model.StadiumName),
                new SqlParameter("@Stadium_IMG", finalImagePath),
                new SqlParameter("@Map_Url", (object?)model.Map_Url ?? DBNull.Value),
                new SqlParameter("@Description", (object?)model.Description ?? DBNull.Value),
                new SqlParameter("@Status", "Pending"),
                new SqlParameter("@Inactive", false),
                new SqlParameter("@Request_Date", currentTime),
                new SqlParameter("@Capacity", model.Capacity),
                new SqlParameter("@ZoneA", model.ZoneA),
                new SqlParameter("@ZoneB", model.ZoneB),
                new SqlParameter("@Stadium_Location", model.Stadium_Location)
            };

            await _db.ExecuteDB("STADIUM_REQUEST", requestParameters);
            TempData["Success"] = "Your stadium edit request has been submitted to the admin for approval.";
            return RedirectToAction(nameof(StadiumManage));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,ClubOwner")]
        public async Task<IActionResult> StadiumManage()
        {
            var userIdString = _userManager.GetUserId(User);
            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                return Forbid();
            }

            DataTable stadiumsTable;
            if (User.IsInRole("Admin"))
            {
                stadiumsTable = (DataTable)await _db.ExecuteDB("GET_ALL_STADIUMS", null, returnTable: true);
            }
            else
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@User_ID", userId)
                };
                stadiumsTable = (DataTable)await _db.ExecuteDB("CLUBOWNER_STADIUM_SHOW", param, returnTable: true);
            }

            return View(stadiumsTable);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var userIdString = _userManager.GetUserId(User);
            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                return Forbid();
            }

            SqlParameter[] paramsToggle = new SqlParameter[]
            {
                new SqlParameter("@Stadium_ID", id),
                new SqlParameter("@Updated_Date", DateTime.Now),
                new SqlParameter("@Updated_UID", userId)
            };

            await _db.ExecuteDB("TOGGLE_STADIUM_STATUS", paramsToggle);

            TempData["Success"] = "Stadium status updated successfully!";
            return RedirectToAction(nameof(StadiumManage));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,ClubOwner")]
        public IActionResult StadiumRequest()
        {
            return View(new StadiumRequestViewModel());
        }

        [HttpPost]
        [Authorize(Roles = "Admin,ClubOwner")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StadiumRequest(StadiumRequestViewModel model, IFormFile? stadiumFile, [FromServices] IWebHostEnvironment env)
        {
            var userIdString = _userManager.GetUserId(User);
            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                return Forbid();
            }

            if ((model.ZoneA + model.ZoneB) > model.Capacity)
            {
                ModelState.AddModelError(string.Empty, "The combined capacity of Zone A and Zone B cannot exceed the Total Stadium Capacity.");
            }

            var nameCheckParam = new SqlParameter("@Stadium_Name", model.StadiumName ?? string.Empty);
            var existsParam = new SqlParameter("@Exists", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            await _db.ExecuteDB("CHECK_STADIUM_NAME_EXISTS", new SqlParameter[] { nameCheckParam, existsParam });

            if (existsParam.Value != null && existsParam.Value != DBNull.Value && Convert.ToBoolean(existsParam.Value))
            {
                ModelState.AddModelError("StadiumName", "A stadium with this name already exists or has a pending request.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string finalImagePath = "/images/default-stadium.png";
            if (stadiumFile != null && stadiumFile.Length > 0)
            {
                var webRootPath = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadsFolder = Path.Combine(webRootPath, "uploads", "stadiums");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(stadiumFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await stadiumFile.CopyToAsync(fileStream);
                }
                finalImagePath = "/uploads/stadiums/" + uniqueFileName;
            }

            var currentTime = DateTime.Now;

            if (User.IsInRole("Admin"))
            {
                SqlParameter[] adminParams = new SqlParameter[]
                {
                    new SqlParameter("@User_ID", userId),
                    new SqlParameter("@Stadium_Name", model.StadiumName),
                    new SqlParameter("@Stadium_IMG", finalImagePath),
                    new SqlParameter("@Map_Url", (object?)model.Map_Url ?? DBNull.Value),
                    new SqlParameter("@Description", (object?)model.Description ?? DBNull.Value),
                    new SqlParameter("@Inactive", false),
                    new SqlParameter("@Created_Date", currentTime),
                    new SqlParameter("@Updated_UID", userId),
                    new SqlParameter("@Updated_Date", currentTime),
                    new SqlParameter("@Capacity", model.Capacity),
                    new SqlParameter("@ZoneA", model.ZoneA),
                    new SqlParameter("@ZoneB", model.ZoneB),
                    new SqlParameter("@Stadium_Location", model.Stadium_Location)
                };

                await _db.ExecuteDB("ADD_STADIUM", adminParams);

                TempData["Success"] = "Stadium created successfully!";
                return RedirectToAction(nameof(StadiumManage));
            }

            var pendingTable = (DataTable)await _db.ExecuteDB("CHECK_PENDING_STADIUM_REQUEST", new SqlParameter[] { new SqlParameter("@User_ID", userId) }, returnTable: true);

            if (pendingTable != null && pendingTable.Rows.Count > 0)
            {
                int pendingCount = Convert.ToInt32(pendingTable.Rows[0]["PendingCount"]);
                if (pendingCount > 0)
                {
                    TempData["Error"] = "You already have a pending stadium request. Please wait for the admin to review it.";
                    return View(model);
                }
            }

            SqlParameter[] requestParameters = new SqlParameter[]
            {
                new SqlParameter("@Stadium_ID", DBNull.Value),
                new SqlParameter("@User_ID", userId),
                new SqlParameter("@Request_Type", model.RequestType ?? "NewStadium"),
                new SqlParameter("@Stadium_Name", model.StadiumName),
                new SqlParameter("@Stadium_IMG", finalImagePath),
                new SqlParameter("@Map_Url", (object?)model.Map_Url ?? DBNull.Value),
                new SqlParameter("@Description", (object?)model.Description ?? DBNull.Value),
                new SqlParameter("@Status", "Pending"),
                new SqlParameter("@Inactive", false),
                new SqlParameter("@Request_Date", currentTime),
                new SqlParameter("@Capacity", model.Capacity),
                new SqlParameter("@ZoneA", model.ZoneA),
                new SqlParameter("@ZoneB", model.ZoneB),
                new SqlParameter("@Stadium_Location", model.Stadium_Location)
            };

            await _db.ExecuteDB("STADIUM_REQUEST", requestParameters);

            TempData["Success"] = "Your stadium request has been submitted successfully and is pending approval.";
            return RedirectToAction(nameof(StadiumManage));
        }
    }
}