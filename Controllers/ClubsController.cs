using _bk12.Data;
using _bk12.Models;
using _bk12.Services;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System.Data;
using ApplicationUser = _bk12.Data.ApplicationUser;

namespace _bk12.Controllers
{

    [Authorize]
    public class ClubsController : Controller
    {
        private readonly Connection _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private bool TryGetUserId(out Guid userId)
        {
            userId = Guid.Empty;
            var userIdString = _userManager.GetUserId(User);
            return Guid.TryParse(userIdString, out userId);
        }

        public ClubsController(
            Connection db,
            UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult DebugRole()
        {
            return Json(new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated,
                IsAdmin = User.IsInRole("Admin"),
                IsClubOwner = User.IsInRole("ClubOwner"),
                IsStaff = User.IsInRole("Staff"),
                IsUser = User.IsInRole("User"),
                Claims = User.Claims.Select(c => new { c.Type, c.Value })
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,ClubOwner")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int club_ID, string clubName, string? description, string? logoUrl, IFormFile? logoFile, string? selectedUserId, [FromServices] IWebHostEnvironment env)
        {
            string finalLogoPath = logoUrl ?? "/images/default-club.png";
            if (logoFile != null && logoFile.Length > 0)
            {
                var webRootPath = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadsFolder = Path.Combine(webRootPath, "uploads", "clubs");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(logoFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(fileStream);
                }
                finalLogoPath = "/uploads/clubs/" + uniqueFileName;
            }

            var currentUserId = _userManager.GetUserId(User);
            if (!Guid.TryParse(currentUserId, out Guid userId))
            {
                return Forbid();
            }

            var currentTime = DateTime.Now;

            if (User.IsInRole("ClubOwner") && !User.IsInRole("Admin"))
            {
                var pendingTable = (DataTable)await _db.ExecuteDB("CHECK_PENDING_CLUB_REQUEST", new SqlParameter[] {new SqlParameter("@User_ID", userId)}, returnTable: true);

                if (pendingTable.Rows.Count > 0)
                {
                    int pendingCount = Convert.ToInt32(pendingTable.Rows[0]["PendingCount"]);
                    if (pendingCount > 0)
                    {
                        TempData["Error"] = "You already have a pending club request. Please wait for the admin to review it before submitting another.";
                        return RedirectToAction(nameof(Manage));
                    }
                }

                SqlParameter[] requestParameters = new SqlParameter[]
                {
                    new SqlParameter("@Club_ID", club_ID),
                    new SqlParameter("@User_ID", userId),
                    new SqlParameter("@Request_Type", "EditClub"),
                    new SqlParameter("@Club_Name", clubName),
                    new SqlParameter("@Description", description ?? (object)DBNull.Value),
                    new SqlParameter("@Status", "Pending"),
                    new SqlParameter("@Submitted_Date", currentTime),
                    new SqlParameter("@Reviewed_by", DBNull.Value),
                    new SqlParameter("@Logo_URL", finalLogoPath),
                    new SqlParameter("@Rejected_Reason", DBNull.Value),
                    new SqlParameter("@Updated_Date", currentTime)
                };

                await _db.ExecuteDB("CLUB_REQUEST", requestParameters);

                TempData["Success"] = "Your club edit request has been submitted to the admin for approval.";
                return RedirectToAction(nameof(Manage));
            }

            Guid targetUserId;
            if (!string.IsNullOrEmpty(selectedUserId) && Guid.TryParse(selectedUserId, out var parsedSelectedUser))
            {
                targetUserId = parsedSelectedUser;
            }
            else
            {
                var existingClubDt = (DataTable)await _db.ExecuteDB("GET_CLUB_BY_ID", new SqlParameter[] { new SqlParameter("@Club_ID", club_ID) }, returnTable: true);
                if (existingClubDt.Rows.Count > 0 && existingClubDt.Rows[0]["User_ID"] != DBNull.Value)
                {
                    targetUserId = Guid.Parse(existingClubDt.Rows[0]["User_ID"].ToString()!);
                }
                else
                {
                    targetUserId = userId;
                }
            }

            SqlParameter[] editParams = new SqlParameter[] {
                new SqlParameter("@Club_ID", club_ID),
                new SqlParameter("@User_ID", targetUserId),
                new SqlParameter("@Club_Name", clubName),
                new SqlParameter("@Description", (object?)description ?? DBNull.Value),
                new SqlParameter("@Logo_Url", finalLogoPath),
                new SqlParameter("@Updated_UID", userId),
                new SqlParameter("@Updated_Date", currentTime)
            };

            await _db.ExecuteDB("ADMIN_EDIT_CLUB_INFORMATION", editParams);

            if (!string.IsNullOrEmpty(selectedUserId))
            {
                SqlParameter[] ownerParams = new SqlParameter[] {
                    new SqlParameter("@Club_ID", club_ID),
                    new SqlParameter("@User_ID", targetUserId),
                    new SqlParameter("@Updated_UID", userId),
                    new SqlParameter("@Updated_Date", currentTime)
                };
                await _db.ExecuteDB("UPDATE_OWNER", ownerParams);
            }

            TempData["SuccessMessage"] = "Club updated successfully!";
            return RedirectToAction("Manage");
        }

        [HttpGet]
        [Authorize(Roles = "Admin,ClubOwner")]
        public async Task<IActionResult> Manage(int? selectedClubId)
        {
            if (!TryGetUserId(out Guid userId))
            {
                return Forbid();
            }

            var viewModel = new ManageClubViewModel();
            var allClubs = new List<Club>();

            if (User.IsInRole("Admin"))
            {
                var table = (DataTable)await _db.ExecuteDB("SHOW_ALL_CLUB_ADMIN" +
                    "", returnTable: true);
                foreach (DataRow row in table.Rows)
                {
                    allClubs.Add(Global.MapClubFromRow(row));
                }

                viewModel.AvailableClubs = allClubs;
                if (allClubs.Any())
                {
                    viewModel.HasClub = true;
                    if (selectedClubId.HasValue)
                    {
                        viewModel.OwnedClub = allClubs.FirstOrDefault(c => c.Club_ID == selectedClubId.Value) ?? allClubs.First();
                    }
                    else
                    {
                        viewModel.OwnedClub = allClubs.First();
                    }
                }
            }
            else
            {
                Club? ownedClub = null;
                var ownedTable = (DataTable)await _db.ExecuteDB("GET_OWNED_CLUB", new SqlParameter[] {
                    new SqlParameter("@User_ID", userId)
                }, returnTable: true);

                if (ownedTable.Rows.Count > 0)
                {
                    ownedClub = Global.MapClubFromRow(ownedTable.Rows[0]);
                }

                if (ownedClub != null)
                {
                    viewModel.HasClub = true;
                    viewModel.OwnedClub = ownedClub;
                }
                else
                {
                    viewModel.HasClub = false;
                    var availableTable = (DataTable)await _db.ExecuteDB("GET_AVAILABLE_CLUBS_FOR_OWNER", returnTable: true);
                    foreach (DataRow row in availableTable.Rows)
                    {
                        allClubs.Add(Global.MapClubFromRow(row));
                    }

                    viewModel.AvailableClubs = allClubs;
                }
            }

            if (viewModel.OwnedClub != null && viewModel.OwnedClub.User_ID.HasValue && viewModel.OwnedClub.User_ID.Value != Guid.Empty)
            {
                var userTable = (DataTable)await _db.ExecuteDB("GET_CLUB_OWNER_USERNAME", new SqlParameter[] {
                    new SqlParameter("@User_ID", viewModel.OwnedClub.User_ID.Value)
                }, returnTable: true);

                if (userTable.Rows.Count > 0)
                {
                    viewModel.OwnerUsername = userTable.Rows[0]["OwnerUsername"] as string ?? "Unknown User";
                }
                else
                {
                    viewModel.OwnerUsername = "Unknown User";
                }
            }
            else
            {
                viewModel.OwnerUsername = "Unassigned";
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClaimExistingClub(int selectedClubId)
        {
            if (!TryGetUserId(out Guid userId))
            {
                return Forbid();
            }

            var clubTable = (DataTable)await _db.ExecuteDB("GET_CLUB_BY_ID", new SqlParameter[] {
                new SqlParameter("@Club_ID", selectedClubId)
            }, returnTable: true);

            if (clubTable.Rows.Count == 0)
            {
                return NotFound("The selected club does not exist.");
            }

            var club = Global.MapClubFromRow(clubTable.Rows[0]);

            if (club.User_ID.HasValue && club.User_ID.Value != Guid.Empty)
            {
                return BadRequest("The selected club is already claimed by another ClubOwner.");
            }

            // Check existing pending request
            var pendingTable = (DataTable)await _db.ExecuteDB("CHECK_PENDING_CLUB_REQUEST", new SqlParameter[] {
                new SqlParameter("@User_ID", userId)
            }, returnTable: true);

            int pendingCount = Convert.ToInt32(pendingTable.Rows[0]["PendingCount"]);
            if (pendingCount > 0)
            {
                return BadRequest("You already have a pending club request. Please wait for it to be processed.");
            }

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Club_ID", selectedClubId),
                new SqlParameter("@User_ID", userId),
                new SqlParameter("@Request_Type", "ClaimClub"),
                new SqlParameter("@Club_Name", club.Club_Name),
                new SqlParameter("@Description", club.Description ?? (object)DBNull.Value),
                new SqlParameter("@Status", "Pending"),
                new SqlParameter("@Submitted_Date", DateTime.Now),
                new SqlParameter("@Reviewed_by", DBNull.Value),
                new SqlParameter("@Reviewed_Date", DBNull.Value),
                new SqlParameter("@Logo_URL", club.Logo_Url ?? (object)DBNull.Value),
                new SqlParameter("@Rejected_Reason", DBNull.Value),
                new SqlParameter("@Updated_Date", DateTime.Now)
            };

            await _db.ExecuteDB("CLUB_REQUEST", parameters);

            TempData["Success"] = "Your request to claim this club has been submitted to the admin.";
            return RedirectToAction(nameof(Manage));
        }
        [HttpGet]
        [Authorize(Roles = "Admin,ClubOwner")]
        public IActionResult ClubRequest()
        {
            return View(new ClubRequestViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ClubOwner")]
        public async Task<IActionResult> ClubRequest(ClubRequestViewModel model, [FromServices] IWebHostEnvironment env)
        {

            bool hasFile = model.LogoFile != null && model.LogoFile.Length > 0;
            bool hasUrl = !string.IsNullOrWhiteSpace(model.Logo_Url);

            if (!hasFile && !hasUrl)
            {
                ModelState.AddModelError("LogoFile", "Please either upload an image file or paste a logo URL.");
                ModelState.AddModelError("Logo_Url", "Please either upload an image file or paste a logo URL.");
            }

            if (hasFile)
            {
                var extension = Path.GetExtension(model.LogoFile!.FileName).ToLowerInvariant();
                var permittedExtensions = new[] { ".png", ".jpg", ".jpeg" };
                if (!permittedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("LogoFile", "Only .png, .jpg, and .jpeg image files are allowed.");
                }

                const long maxFileSize = 10 * 1024 * 1024;
                if (model.LogoFile.Length > maxFileSize)
                {
                    ModelState.AddModelError("LogoFile", "The uploaded image is too large. Maximum allowed size is 10MB.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!TryGetUserId(out Guid userId))
            {
                return Forbid();
            }
            var pendingTable = (DataTable)await _db.ExecuteDB("CHECK_PENDING_CLUB_REQUEST", new SqlParameter[] {new SqlParameter("@User_ID", userId)}, returnTable: true);

            if (pendingTable.Rows.Count > 0)
            {
                int pendingCount = Convert.ToInt32(pendingTable.Rows[0]["PendingCount"]);
                if (pendingCount > 0)
                {
                    ModelState.AddModelError(string.Empty, "You already have a pending club request. Please wait for the admin to review it before submitting another.");
                    return View(model);
                }
            }
            string finalLogoPath = model.Logo_Url ?? string.Empty;
            if (hasFile)
            {
                try
                {
                    var webRootPath = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var uploadsFolder = Path.Combine(webRootPath, "uploads", "clubs");

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.LogoFile!.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.LogoFile.CopyToAsync(fileStream);
                    }

                    finalLogoPath = "/uploads/clubs/" + uniqueFileName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("LogoFile", $"Error saving file: {ex.Message}");
                    return View(model);
                }
            }

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Club_ID", DBNull.Value),
                new SqlParameter("@User_ID", userId),
                new SqlParameter("@Request_Type", "NewClub"),
                new SqlParameter("@Club_Name", model.ClubName),
                new SqlParameter("@Description", model.Description ?? (object)DBNull.Value),
                new SqlParameter("@Status", "Pending"),
                new SqlParameter("@Submitted_Date", DateTime.UtcNow),
                new SqlParameter("@Reviewed_by", DBNull.Value),
                new SqlParameter("@Reviewed_Date", DBNull.Value),
                new SqlParameter("@Logo_URL", finalLogoPath),
                new SqlParameter("@Rejected_Reason", DBNull.Value),
                new SqlParameter("@Updated_Date", DateTime.UtcNow)
            };

            await _db.ExecuteDB("CLUB_REQUEST", parameters);

            TempData["Success"] = "Your club request was submitted.";
            return RedirectToAction("Manage");
        }

        [HttpGet]
        public async Task<IActionResult> GetClubs()
        {
            var clubsList = new List<object>();

            if (User.IsInRole("Admin"))
            {
                var table = (DataTable)await _db.ExecuteDB("SHOW_ALL_CLUB", returnTable: true);
                foreach (DataRow row in table.Rows)
                {
                    clubsList.Add(new
                    {
                        club_ID = Convert.ToInt32(row["Club_ID"]),
                        user_ID = row["User_ID"] != DBNull.Value ? (Guid?)Guid.Parse(row["User_ID"].ToString()!) : null,
                        club_Name = row["Club_Name"] as string,
                        logo_Url = row["Logo_Url"] as string,
                        description = row["Description"] as string,
                        inactive = Convert.ToBoolean(row["Inactive"]),
                        created_Date = Convert.ToDateTime(row["Created_Date"]),
                        updated_Date = row["Updated_Date"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["Updated_Date"]) : null
                    });
                }
                return Json(clubsList);
            }
            else if (User.IsInRole("ClubOwner"))
            {
                if (!TryGetUserId(out Guid userId))
                {
                    return Forbid();
                }
                var ownerName = _userManager.GetUserName(User) ?? "Unknown User";

                var table = (DataTable)await _db.ExecuteDB("GET_OWNED_CLUB", new SqlParameter[] {
                    new SqlParameter("@User_ID", userId)
                }, returnTable: true);

                foreach (DataRow row in table.Rows)
                {
                    clubsList.Add(new
                    {
                        club_ID = Convert.ToInt32(row["Club_ID"]),
                        user_ID = row["User_ID"] != DBNull.Value ? (Guid?)Guid.Parse(row["User_ID"].ToString()!) : null,
                        owner_Name = ownerName,
                        club_Name = row["Club_Name"] as string,
                        logo_Url = row["Logo_Url"] as string,
                        description = row["Description"] as string,
                        inactive = Convert.ToBoolean(row["Inactive"]),
                        created_Date = Convert.ToDateTime(row["Created_Date"]),
                        updated_Date = row["Updated_Date"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["Updated_Date"]) : null
                    });
                }
                return Json(clubsList);
            }

            return Forbid();
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetClubOwners()
        {
            var users = await _userManager.GetUsersInRoleAsync("ClubOwner");
            var result = users.Select(u => new { id = u.Id, email = u.Email });
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(string club_Name, string description, Guid user_ID)
        {
            if (string.IsNullOrWhiteSpace(club_Name))
            {
                return BadRequest("Club name is required.");
            }

            var owner = await _userManager.FindByIdAsync(user_ID.ToString());
            if (owner == null)
            {
                return BadRequest("Club owner does not exist.");
            }
            if (!await _userManager.IsInRoleAsync(owner, "ClubOwner"))
            {
                return BadRequest("Selected user is not a ClubOwner.");
            }

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@User_ID", user_ID),
                new SqlParameter("@Club_Name", club_Name),
                new SqlParameter("@Logo_URL", DBNull.Value),
                new SqlParameter("@Description", description ?? (object)DBNull.Value),
                new SqlParameter("@Created_Date", DateTime.Now),
                new SqlParameter("@Updated_UID", Guid.NewGuid()),
                new SqlParameter("@Updated_Date", DateTime.Now)
            };

            await _db.ExecuteDB("INSERT_CLUB", parameters);

            return Json(new { success = true, message = "Club created successfully." });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var currentUserID = _userManager.GetUserId(User);
            var currentDate = DateTime.Now;
            await _db.ExecuteDB("TOGGLE_CLUB_STATUS", new SqlParameter[]
            {
                new SqlParameter("@Club_ID", id),
                new SqlParameter("@Updated_Date", currentDate),
                new SqlParameter("@Updated_UID", currentUserID)
            });
            TempData["SuccessMessage"] = "Club status updated successfully!";
            return RedirectToAction("Manage");

        }

        [HttpGet("Clubs/ClubEdit/{id:int}")]
        [Authorize(Roles = "Admin,ClubOwner")]
        public async Task<IActionResult> ClubEdit(int id, Guid id1)
        {
            var clubTable = (DataTable)await _db.ExecuteDB("GET_CLUB_BY_ID", new SqlParameter[] {new SqlParameter("@Club_ID", id)}, returnTable: true);

            if (clubTable.Rows.Count == 0)
            {
                return NotFound();
            }

            var row = clubTable.Rows[0];
            var club = Global.MapClubFromRow(row);
            Guid ownerID = club.User_ID.HasValue ? club.User_ID.Value : Guid.Empty;
            var ownerUsernameTable = (DataTable)await _db.ExecuteDB("GET_CLUB_OWNER_USERNAME", new SqlParameter[] {new SqlParameter("@User_ID", ownerID)}, returnTable: true);
            string OwnerUsername = ownerUsernameTable.Rows.Count > 0 ? ownerUsernameTable.Rows[0]["OwnerUsername"].ToString()! : "Unknown";
            ViewBag.OwnerUsername = OwnerUsername;
            ViewBag.ownerID = ownerID;
            if (User.IsInRole("Admin"))
            {
                var clubOwnersTable = (DataTable)await _db.ExecuteDB("GET_USERS_BY_ROLE", new SqlParameter[] {new SqlParameter("@RoleName", "ClubOwner")}, returnTable: true);

                var ownerList = new List<SelectListItem>();
                foreach (DataRow r in clubOwnersTable.Rows)
                {
                    ownerList.Add(new SelectListItem
                    {
                        Value = r["Id"].ToString(),
                        Text = r["UserName"].ToString(),
                        Selected = Guid.TryParse(r["Id"].ToString(), out Guid parsedId) && parsedId == ownerID
                    });
                }
                ViewBag.ClubOwnersList = ownerList;
            }
            var viewModel = new ClubRequestViewModel
            {
                Club_ID = club.Club_ID,
                ClubName = club.Club_Name,
                Description = club.Description,
                Logo_Url = club.Logo_Url
            };
            return View(viewModel);

        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approval()
        {
            var pendingRequests = new List<ClubRequest>();

            var table = (DataTable)await _db.ExecuteDB("GET_PENDING_CLUB_REQUESTS", returnTable: true);
            foreach (DataRow row in table.Rows)
            {
                pendingRequests.Add(new ClubRequest
                {
                    Id = Convert.ToInt32(row["Club_Request_ID"]),
                    UserId = row["User_ID"] != DBNull.Value ? (Guid?)Guid.Parse(row["User_ID"].ToString()!) : null,
                    Request_Type = row["Request_Type"] as string,
                    ClubId = row["Club_ID"] != DBNull.Value ? (int?)Convert.ToInt32(row["Club_ID"]) : null,
                    ClubName = row["Club_Name"] as string,
                    Description = row["Description"] as string,
                    Status = row["Status"] as string,
                    SubmittedDate = Convert.ToDateTime(row["Submitted_Date"]),
                    Logo_Url = row["Logo_Url"] as string
                });
            }

            return View(pendingRequests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRequestStatus(int id, string actionType, string? rejectedReason)
        {
            var requestTable = (DataTable)await _db.ExecuteDB("GET_CLUB_REQUEST_BY_ID", new SqlParameter[] {
                new SqlParameter("@Club_Request_ID", id)
            }, returnTable: true);

            if (requestTable.Rows.Count == 0)
            {
                return NotFound();
            }

            var row = requestTable.Rows[0];
            var request = new ClubRequest
            {
                Id = Convert.ToInt32(row["Club_Request_ID"]),
                UserId = row["User_ID"] != DBNull.Value ? (Guid?)Guid.Parse(row["User_ID"].ToString()!) : null,
                Request_Type = row["Request_Type"] as string,
                ClubId = row["Club_ID"] != DBNull.Value ? (int?)Convert.ToInt32(row["Club_ID"]) : null,
                ClubName = row["Club_Name"] as string,
                Description = row["Description"] as string,
                Status = row["Status"] as string,
                Logo_Url = row["Logo_Url"] as string
            };

            DateTime reviewTimestamp = DateTime.Now;
            string adminUsername = User.Identity?.Name ?? "Admin";

            if (actionType == "Approve")
            {
                Guid assignedUserId = request.UserId ?? Guid.Empty;
                if (assignedUserId == Guid.Empty)
                {
                    assignedUserId = Guid.NewGuid();
                }

                if (request.Request_Type == "NewClub")
                {
                    SqlParameter[] insertParams = new SqlParameter[]
                    {
                        new SqlParameter("@User_ID", assignedUserId),
                        new SqlParameter("@Club_Name", request.ClubName ?? (object)DBNull.Value),
                        new SqlParameter("@Logo_URL", request.Logo_Url ?? (object)DBNull.Value),
                        new SqlParameter("@Description", request.Description ?? (object)DBNull.Value),
                        new SqlParameter("@Created_Date", reviewTimestamp),
                        new SqlParameter("@Updated_UID", assignedUserId),
                        new SqlParameter("@Updated_Date", reviewTimestamp)
                    };

                    await _db.ExecuteDB("INSERT_CLUB", insertParams);
                }
                else if (request.Request_Type == "ClaimClub")
                {
                    SqlParameter[] updateParams = new SqlParameter[]
                    {
                        new SqlParameter("@Club_ID", request.ClubId ?? 0),
                        new SqlParameter("@User_ID", assignedUserId),
                        new SqlParameter("@Updated_UID", assignedUserId),
                        new SqlParameter("@Updated_Date", reviewTimestamp)
                    };

                    await _db.ExecuteDB("UPDATE_OWNER", updateParams);
                }
                else if (request.Request_Type == "EditClub")
                {
                    SqlParameter[] editParameter = new SqlParameter[]
                    {
                        new SqlParameter("@Club_ID", request.ClubId ?? 0),
                        new SqlParameter("@User_ID", assignedUserId),
                        new SqlParameter("@Club_Name", request.ClubName ?? (object)DBNull.Value),
                        new SqlParameter("@Logo_URL", request.Logo_Url ?? (object)DBNull.Value),
                        new SqlParameter("@Description", request.Description ?? (object)DBNull.Value),
                        new SqlParameter("@Updated_UID", assignedUserId),
                        new SqlParameter("@Updated_Date", reviewTimestamp)
                    };
                    await _db.ExecuteDB("ADMIN_EDIT_CLUB_INFORMATION", editParameter);
                }

                request.Status = "Approved";
                request.Rejected_Reason = null;
            }
            else if (actionType == "Reject")
            {
                request.Status = "Rejected";
                request.Rejected_Reason = rejectedReason;
            }
            else
            {
                return BadRequest("Invalid action type.");
            }

            SqlParameter[] statusParams = new SqlParameter[]
            {
                new SqlParameter("@Club_Request_ID", request.Id),
                new SqlParameter("@Status", request.Status),
                new SqlParameter("@Reviewed_by", adminUsername),
                new SqlParameter("@Reviewed_Date", reviewTimestamp),
                new SqlParameter("@Rejected_Reason", (object?)request.Rejected_Reason ?? DBNull.Value),
                new SqlParameter("@Updated_Date", reviewTimestamp)
            };

            await _db.ExecuteDB("UPDATE_CLUB_REQUEST_STATUS", statusParams);

            TempData["Success"] = $"Club request has been {request.Status.ToLower()}.";
            return RedirectToAction(nameof(Approval));
        }
    }
}