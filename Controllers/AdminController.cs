using _bk12.Data;
using _bk12.Models;
using _bk12.Services;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ApplicationUser = _bk12.Data.ApplicationUser;

namespace _bk12.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly Connection _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(Connection db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }
        [HttpGet]
        public async Task<IActionResult> GetMatchJson(int id)
        {
            SqlParameter[] param = { new SqlParameter("@Match_ID", id) };
            var dt = (DataTable)await _db.ExecuteDB("GET_MATCH_BY_ID", param, returnTable: true);

            if (dt != null && dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                DateTime schedule = row["Schedule"] != DBNull.Value ? Convert.ToDateTime(row["Schedule"]) : DateTime.Now;

                return Json(new
                {
                    success = true,
                    requestedBy = row["Requested_By_Username"]?.ToString() ?? "Unknown User",
                    approvedBy = row["Approved_By_Username"]?.ToString() ?? "System Admin",
                    dateStr = schedule.ToString("dd-MMM-yyyy"),
                    timeStr = schedule.ToString("h:mm tt"),
                    status = row["Status"]?.ToString() ?? "Upcoming",
                    homeClubName = row["Home_Club_Name"]?.ToString() ?? "Home Club",
                    homeClubLogo = row["Home_Club_Logo"]?.ToString() ?? "/images/default-club.png",
                    awayClubName = row["Away_Club_Name"]?.ToString() ?? "Away Club",
                    awayClubLogo = row["Away_Club_Logo"]?.ToString() ?? "/images/default-club.png",
                    stadiumName = row["Stadium_Name"]?.ToString() ?? "Stadium",
                    stadiumImg = row["Stadium_IMG"]?.ToString() ?? "/images/default-stadium.png",
                    stadiumLocation = row["Stadium_Location"]?.ToString() ?? "Location unavailable",
                    note = row["Add_Note"]?.ToString()
                });
            }

            return Json(new { success = false });
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approvals(string tab = "clubs")
        {
            string normalizedTab = tab?.ToLower();
            ViewBag.CurrentTab = normalizedTab == "stadiums" ? "stadiums" : normalizedTab == "matches" ? "matches" : "clubs";

            if (ViewBag.CurrentTab == "stadiums")
            {
                var stadiumRequestsTable = (DataTable)await _db.ExecuteDB("GET_PENDING_STADIUM_REQUEST", null, returnTable: true);
                return View(stadiumRequestsTable);
            }

            if (ViewBag.CurrentTab == "matches")
            {
                var matchTable = (DataTable)await _db.ExecuteDB("GET_ALL_MATCH_REQUESTS_ADMIN", null, returnTable: true);
                var matchRequests = new List<MatchRequest>();

                foreach (DataRow row in matchTable.Rows)
                {
                    matchRequests.Add(MapMatchRequest(row, matchTable));
                }

                return View(matchRequests);
            }

            var pendingRequests = new List<_bk12.Data.ClubRequest>();
            var clubTable = (DataTable)await _db.ExecuteDB("GET_PENDING_CLUB_REQUESTS", null, returnTable: true);

            foreach (DataRow row in clubTable.Rows)
            {
                pendingRequests.Add(new _bk12.Data.ClubRequest
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


        private MatchRequest MapMatchRequest(DataRow row, DataTable table)
        {
            string ResolveImagePath(string columnName, string defaultPath)
            {
                if (!table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
                    return defaultPath;

                string path = row[columnName].ToString()?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(path) || path.Equals("null", StringComparison.OrdinalIgnoreCase))
                    return defaultPath;

                return (!path.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !path.StartsWith("/"))
                    ? "/" + path
                    : path;
            }

            T GetValue<T>(string columnName, T defaultValue)
            {
                if (!table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
                    return defaultValue;
                return (T)Convert.ChangeType(row[columnName], typeof(T));
            }

            string GetString(string columnName, string defaultValue)
            {
                if (!table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
                    return defaultValue;
                return row[columnName].ToString() ?? defaultValue;
            }

            int homeClubId = GetValue<int>("Home_Club", 0);
            int awayClubId = GetValue<int>("Away_Club", 0);
            int stadiumId = GetValue<int>("Stadium_ID", 0);

            return new MatchRequest
            {
                Request_ID = GetValue<int>("Request_ID", 0),
                Match_ID = table.Columns.Contains("Match_ID") && row["Match_ID"] != DBNull.Value ? (int?)Convert.ToInt32(row["Match_ID"]) : null,
                Request_Type = GetString("Request_Type", "NewMatch"),

                Home_Club = homeClubId,
                Home_Club_Name = GetString("Home_Club_Name", $"Club #{homeClubId}"),
                Home_Logo = ResolveImagePath("Home_Logo", "/images/default-club.png"),

                Away_Club = awayClubId,
                Away_Club_Name = GetString("Away_Club_Name", $"Club #{awayClubId}"),
                Away_Logo = ResolveImagePath("Away_Logo", "/images/default-club.png"),

                Stadium_ID = stadiumId,
                Stadium_Name = GetString("Stadium_Name", $"Stadium #{stadiumId}"),
                Stadium_IMG = ResolveImagePath("Stadium_IMG", "/images/default-stadium.png"),
                Stadium_Location = GetString("Stadium_Location", "Phnom Penh, Cambodia"),

                User_ID = GetString("User_ID", "System"),
                Schedule = GetValue<DateTime>("Schedule", DateTime.MinValue),
                Status = GetString("Status", "Pending"),
                Requested_Date = GetValue<DateTime>("Requested_Date", DateTime.MinValue),
                Add_Note = GetString("Add_Note", "No additional note provided.")
            };
        }


        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approved(string tab = "clubs")
        {
            ViewBag.CurrentTab = tab?.ToLower() == "stadiums" ? "stadiums" : tab?.ToLower() == "matches" ? "matches" : "clubs";
            var clubTable = (DataTable)await _db.ExecuteDB("SHOW_ALL_CLUB_ADMIN", returnTable: true);
            var clubList = new List<Club>();
            foreach (DataRow row in clubTable.Rows)
            {
                clubList.Add(Global.MapClubFromRow(row));
            }
            ViewBag.Clubs = clubList;

            var stadiumTable = (DataTable)await _db.ExecuteDB("GET_ALL_STADIUMS", returnTable: true);
            ViewBag.StadiumsTable = stadiumTable;

            var matchTable = (DataTable)await _db.ExecuteDB("SHOW_ALL_MATCH_ADMIN", returnTable: true);
            ViewBag.MatchTable = matchTable;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Requests()
        {
            var allRequests = new List<ClubRequestViewModel>();

            var table = (DataTable)await _db.ExecuteDB("GET_ALL_CLUB_REQUESTS", returnTable: true);
            var users = _userManager.Users.ToList();
            var userDict = users.ToDictionary(u => u.Id.ToString(), u => u.UserName ?? "Unknown");

            foreach (DataRow row in table.Rows)
            {
                var userIdStr = row["User_ID"] != DBNull.Value ? row["User_ID"].ToString() : string.Empty;

                allRequests.Add(new ClubRequestViewModel
                {
                    Club_ID = Convert.ToInt32(row["Club_Request_ID"]),
                    Username = !string.IsNullOrEmpty(userIdStr) && userDict.ContainsKey(userIdStr) ? userDict[userIdStr] : "Unknown",
                    ClubName = row["Club_Name"] as string,
                    Description = row["Description"] as string,
                    Logo_Url = row["Logo_Url"] as string,
                    SubmittedDate = Convert.ToDateTime(row["Submitted_Date"]),
                    ReviewedDate = row["Updated_Date"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["Updated_Date"]) : null,
                    ReviewedBy = row["Reviewed_By"] as string ?? "-",
                    Status = row["Status"] as string,
                    RejectedReason = row["Rejected_Reason"] as string
                });
            }

            return View(allRequests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRequestStatus(int id, string actionType, string? rejectedReason, string tab = "clubs")
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
                Logo_Url = row["Logo_Url"] as string,
                Status = row["Status"] as string
            };

            DateTime reviewTimestamp = DateTime.Now;
            string adminUsername = User.Identity?.Name ?? "Admin";
            string newStatus = "";
            string? finalRejectedReason = null;

            if (actionType == "Approve")
            {
                Guid assignedUserId = request.UserId ?? Guid.Empty;
                if (assignedUserId == Guid.Empty)
                {
                    assignedUserId = Guid.NewGuid();
                }

                newStatus = "Approved";

                if (request.Request_Type == "NewClub")
                {
                    SqlParameter[] insertParams = _db.CreateParams
                    (
                        ("@User_ID", assignedUserId),
                        ("@Club_Name", request.ClubName ?? (object)DBNull.Value),
                        ("@Logo_URL", request.Logo_Url ?? (object)DBNull.Value),
                        ("@Description", request.Description ?? (object)DBNull.Value),
                        ("@Created_Date", reviewTimestamp),
                        ("@Updated_UID", assignedUserId),
                        ("@Updated_Date", reviewTimestamp)
                    );

                    await _db.ExecuteDB("INSERT_CLUB", insertParams);
                }
                else if (request.Request_Type == "ClaimClub")
                {
                    SqlParameter[] updateParams = _db.CreateParams
                    (
                        ("@Club_ID", request.ClubId ?? 0),
                        ("@User_ID", assignedUserId),
                        ("@Updated_UID", assignedUserId),
                        ("@Updated_Date", reviewTimestamp)
                    );

                    await _db.ExecuteDB("UPDATE_OWNER", updateParams);
                }
                else if (request.Request_Type == "EditClub")
                {
                    SqlParameter[] editParameter = _db.CreateParams
                    (
                        ("@Club_ID", request.ClubId ?? 0),
                        ("@User_ID", assignedUserId),
                        ("@Club_Name", request.ClubName ?? (object)DBNull.Value),
                        ("@Logo_URL", request.Logo_Url ?? (object)DBNull.Value),
                        ("@Description", request.Description ?? (object)DBNull.Value),
                        ("@Updated_UID", assignedUserId),
                        ("@Updated_Date", reviewTimestamp)
                    );
                    await _db.ExecuteDB("ADMIN_EDIT_CLUB_INFORMATION", editParameter);
                }
                else
                {
                    return BadRequest("Invalid request type for approval.");
                }
            }
            else if (actionType == "Reject")
            {
                newStatus = "Rejected";
                finalRejectedReason = rejectedReason;
            }
            else
            {
                return BadRequest("Invalid action type.");
            }

            SqlParameter[] statusParams = _db.CreateParams
            (
                ("@Club_Request_ID", request.Id),
                ("@Status", newStatus),
                ("@Reviewed_by", adminUsername),
                ("@Rejected_Reason", (object?)finalRejectedReason ?? DBNull.Value),
                ("@Updated_Date", reviewTimestamp)
            );

            await _db.ExecuteDB("UPDATE_CLUB_REQUEST_STATUS", statusParams);

            TempData["Success"] = $"Club request has been {newStatus.ToLower()}.";

            return RedirectToAction(nameof(Approvals), new { tab = tab });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStadiumRequestStatus(int id, string actionType, string? rejectedReason, string tab = "stadiums")
        {
            var userIdString = _userManager.GetUserId(User);
            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                return Forbid();
            }

            var requestTable = (DataTable)await _db.ExecuteDB("GET_STADIUM_REQUEST_BY_ID", new SqlParameter[] {new SqlParameter("@Request_ID", id)}, returnTable: true);

            if (requestTable == null || requestTable.Rows.Count == 0)
            {
                return NotFound();
            }

            var row = requestTable.Rows[0];
            var request = new StadiumRequest
            {
                Id = Convert.ToInt32(row["Request_ID"]),
                UserId = row["User_ID"] != DBNull.Value ? (Guid?)Guid.Parse(row["User_ID"].ToString()!) : null,
                Request_Type = row["Request_Type"] as string,
                Stadium_ID = row["Stadium_ID"] != DBNull.Value ? (int?)Convert.ToInt32(row["Stadium_ID"]) : null,
                Stadium_Name = row["Stadium_Name"] as string,
                Description = row["Description"] as string,
                Stadium_IMG = row["Stadium_IMG"] as string,
                Map_Url = row["Map_Url"] as string,
                Capacity = row["Capacity"] != DBNull.Value ? (int?)Convert.ToInt32(row["Capacity"]) : null,
                ZoneA = row["ZoneA"] != DBNull.Value ? (int?)Convert.ToInt32(row["ZoneA"]) : null,
                ZoneB = row["ZoneB"] != DBNull.Value ? (int?)Convert.ToInt32(row["ZoneB"]) : null,
                Status = row["Status"] as string
            };

            string newStatus = actionType == "Approve" ? "Approved" : "Rejected";
            string? finalRejectedReason = actionType == "Reject" ? rejectedReason : null;
            DateTime reviewTimestamp = DateTime.Now;

            if (actionType == "Approve")
            {
                if (request.Request_Type == "NewStadium")
                {
                    SqlParameter[] insertParams = _db.CreateParams
                    (
                        ("@User_ID", request.UserId ?? (object)DBNull.Value),
                        ("@Stadium_Name", request.Stadium_Name ?? (object)DBNull.Value),
                        ("@Stadium_IMG", request.Stadium_IMG ?? (object)DBNull.Value),
                        ("@Map_Url", (object?)request.Map_Url ?? DBNull.Value),
                        ("@Description", (object?)request.Description ?? DBNull.Value),
                        ("@Inactive", false),
                        ("@Created_Date", reviewTimestamp),
                        ("@Updated_Date", reviewTimestamp),
                        ("@Updated_UID", userId),
                        ("@Capacity", request.Capacity ?? 0),
                        ("@ZoneA", request.ZoneA ?? 0),
                        ("@ZoneB", request.ZoneB ?? 0)
                    );
                    await _db.ExecuteDB("ADD_STADIUM", insertParams);
                }
                else if (request.Request_Type == "EditStadium")
                {
                    if (request.Stadium_ID == null || request.Stadium_ID <= 0)
                    {
                        return BadRequest("Cannot update stadium: Stadium ID is missing or invalid for this request.");
                    }

                    SqlParameter[] editParams = _db.CreateParams
                    (
                        ("@Stadium_ID", id),
                        ("@Stadium_Name", request.Stadium_Name ?? (object)DBNull.Value),
                        ("@Stadium_IMG", request.Stadium_IMG ?? (object)DBNull.Value),
                        ("@Map_Url", (object?)request.Map_Url ?? DBNull.Value),
                        ("@Description", (object?)request.Description ?? DBNull.Value),
                        ("@Updated_UID", userId),
                        ("@Updated_Date", reviewTimestamp),
                        ("@Capacity", request.Capacity ?? 0),
                        ("@ZoneA", request.ZoneA ?? 0),
                        ("@ZoneB", request.ZoneB ?? 0),
                        ("@Stadium_Location", request.Stadium_Location)
                    );
                    await _db.ExecuteDB("ADMIN_EDIT_STADIUM", editParams);
                }
            }

            SqlParameter[] statusParams = _db.CreateParams
            (
                ("@Request_ID", id),
                ("@Status", newStatus),
                ("@Rejected_Reason", (object?)finalRejectedReason ?? DBNull.Value),
                ("@Updated_Date", reviewTimestamp),
                ("@Updated_UID", userId)
            );

            await _db.ExecuteDB("UPDATE_STADIUM_REQUEST_STATUS", statusParams);

            TempData["Success"] = $"Stadium request has been {newStatus.ToLower()} successfully.";

            return RedirectToAction(nameof(Approvals), new { tab = tab });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMatchRequestStatus(int id, string actionType, string? rejectedReason, string tab = "match")
        {
            var userIdString = _userManager.GetUserId(User);
            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                return Forbid();
            }

            var requestTable = (DataTable)await _db.ExecuteDB("GET_MATCH_REQUEST_BY_ID", new SqlParameter[] { new SqlParameter("@Request_ID", id) }, returnTable: true);

            if (requestTable == null || requestTable.Rows.Count == 0)
            {
                return NotFound();
            }

            var row = requestTable.Rows[0];
            var matchRequest = new MatchRequest
            {
                Request_ID = Convert.ToInt32(row["Request_ID"]),
                Match_ID = row.Table.Columns.Contains("Match_ID") && row["Match_ID"] != DBNull.Value? (int?)Convert.ToInt32(row["Match_ID"]): null,

                Request_Type = row.Table.Columns.Contains("Request_Type") && row["Request_Type"] != DBNull.Value? row["Request_Type"].ToString(): "NewMatch",

                Home_Club = Convert.ToInt32(row["Home_Club"]),
                Home_Club_Name = row.Table.Columns.Contains("Home_Club_Name") && row["Home_Club_Name"] != DBNull.Value? row["Home_Club_Name"].ToString(): null,
                Home_Logo = row.Table.Columns.Contains("Home_Logo") && row["Home_Logo"] != DBNull.Value? row["Home_Logo"].ToString(): "/images/default-club.png",

                Away_Club = Convert.ToInt32(row["Away_Club"]),
                Away_Club_Name = row.Table.Columns.Contains("Away_Club_Name") && row["Away_Club_Name"] != DBNull.Value? row["Away_Club_Name"].ToString(): null,
                Away_Logo = row.Table.Columns.Contains("Away_Logo") && row["Away_Logo"] != DBNull.Value? row["Away_Logo"].ToString(): "/images/default-club.png",

                Stadium_ID = Convert.ToInt32(row["Stadium_ID"]),
                Stadium_Name = row.Table.Columns.Contains("Stadium_Name") && row["Stadium_Name"] != DBNull.Value? row["Stadium_Name"].ToString(): null,
                Stadium_IMG = row.Table.Columns.Contains("Stadium_IMG") && row["Stadium_IMG"] != DBNull.Value? row["Stadium_IMG"].ToString(): "/images/default-stadium.png",
                Stadium_Location = row.Table.Columns.Contains("Stadium_Location") && row["Stadium_Location"] != DBNull.Value? row["Stadium_Location"].ToString(): "Phnom Penh, Cambodia",

                User_ID = row["User_ID"] != DBNull.Value ? row["User_ID"].ToString() : null,
                Schedule = row["Schedule"] != DBNull.Value ? Convert.ToDateTime(row["Schedule"]) : DateTime.MinValue,
                Status = row["Status"] != DBNull.Value ? row["Status"].ToString() : "Pending",
                Requested_Date = row.Table.Columns.Contains("Requested_Date") && row["Requested_Date"] != DBNull.Value? Convert.ToDateTime(row["Requested_Date"]): DateTime.MinValue,
                Add_Note = row.Table.Columns.Contains("Add_Note") && row["Add_Note"] != DBNull.Value? row["Add_Note"].ToString(): null
            };

            string newStatus = actionType == "Approve" ? "Approved" : "Rejected";
            string? finalRejectedReason = actionType == "Reject" ? rejectedReason : null;
            DateTime reviewTimestamp = DateTime.Now;

            if (actionType == "Approve")
            {
                if (matchRequest.Request_Type == "NewMatch")
                {
                    SqlParameter[] parameter = _db.CreateParams
                    (
                        ("@Home_Club", matchRequest.Home_Club),
                        ("@Away_Club", matchRequest.Away_Club),
                        ("@Stadium_ID", matchRequest.Stadium_ID),
                        ("@User_ID", matchRequest.User_ID),
                        ("@Schedule", matchRequest.Schedule),
                        ("@Updated_Date", matchRequest.Updated_Date),
                        ("@Updated_UID", userId),
                        ("@Add_Note", matchRequest.Add_Note)
                    );

                    await _db.ExecuteDB("ADD_MATCH", parameter);

                    SqlParameter[] sqlParameters = _db.CreateParams
                    (
                        ("@Request_ID", matchRequest.Request_ID),
                        ("@Match_ID", matchRequest.Request_ID),
                        ("@Status", "Approved"),
                        ("@Rejected_Reason", null),
                        ("@Updated_Date", DateTime.Now),
                        ("@Updated_UID", matchRequest.Updated_UID),
                        ("@Add_Note", matchRequest.Add_Note)
                    );

                    await _db.ExecuteDB("UPDATE_MATCH_REQUEST_STATUS", sqlParameters);
                }
                else if (matchRequest.Request_Type == "EditMatchRequest")
                {

                    SqlParameter[] editParams = _db.CreateParams
                    (
                        ("@Request_ID", matchRequest.Request_ID),
                        ("@Home_Club", matchRequest.Home_Club),
                        ("@Away_Club", matchRequest.Away_Club),
                        ("@Stadium_ID", matchRequest.Stadium_ID),
                        ("@User_ID", matchRequest.User_ID),
                        ("@Schedule", matchRequest.Schedule),
                        ("@Updated_Date", matchRequest.Updated_Date),
                        ("@Updated_UID", userId),
                        ("@Add_Note", matchRequest.Add_Note),
                        ("@Rejected_Reason", matchRequest.Rejected_Reason)
                    );
                    await _db.ExecuteDB("UPDATE_MATCH_REQUEST_STATUS", editParams);
                }
            }
            else
            {
                SqlParameter[] sqlParameters = _db.CreateParams
                    (
                        ("@Request_ID", matchRequest.Request_ID),
                        ("@Match_ID", matchRequest.Request_ID),
                        ("@Status", "Rejected"),
                        ("@Rejected_Reason", matchRequest.Rejected_Reason),
                        ("@Updated_Date", DateTime.Now),
                        ("@Updated_UID", matchRequest.Updated_UID),
                        ("@Add_Note", matchRequest.Add_Note)
                    );

                await _db.ExecuteDB("UPDATE_MATCH_REQUEST_STATUS", sqlParameters);
            }

            TempData["Success"] = $"Match request has been {newStatus.ToLower()} successfully.";

            return RedirectToAction(nameof(Approvals), new { tab = tab });
        }
    }
}