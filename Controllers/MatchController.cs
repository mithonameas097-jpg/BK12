using _bk12.Data;
using _bk12.Models; // Ensure your models namespace is included
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace _bk12.Controllers
{
    [Authorize]
    public class MatchController : Controller
    {
        private readonly Connection _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public MatchController(Connection db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,ClubOwner")]
        public async Task<IActionResult> MatchRequest()
        {
            await PopulateClubsAndStadiumsViewBags();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ClubOwner")]
        public async Task<IActionResult> CreateMatchRequest(int Home_Club, int Away_Club, int Stadium_ID, DateTime MatchDate, TimeSpan MatchTime, string? Note)
        {
            if (Home_Club == Away_Club)
            {
                ModelState.AddModelError("", "The home and away clubs must be different.");
            }

            var userIdString = _userManager.GetUserId(User);
            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                return Forbid();
            }

            DateTime schedule = MatchDate.Date + MatchTime;
            if (schedule < DateTime.Now)
            {
                ModelState.AddModelError("", "The selected match date and time must be in the future.");
            }

            if (ModelState.IsValid)
            {
                SqlParameter[] parameters = new SqlParameter[] {
                    new SqlParameter("@Request_Type", "NewMatch"),
                    new SqlParameter("@Home_Club", Home_Club),
                    new SqlParameter("@Away_Club", Away_Club),
                    new SqlParameter("@Stadium_ID", Stadium_ID),
                    new SqlParameter("@User_ID", userId),
                    new SqlParameter("@Schedule", schedule),
                    new SqlParameter("@Status", "Pending"),
                    new SqlParameter("@Requested_Date", DateTime.Now),
                    new SqlParameter("@Add_Note", (object?)Note ?? DBNull.Value),
                    new SqlParameter("@Temp_Req_ID", DBNull.Value)
                };

                var resultTable = (DataTable)await _db.ExecuteDB("INSERT_MATCH_REQUEST", parameters, returnTable: true);
                if (resultTable != null && resultTable.Rows.Count > 0)
                {
                    int Success = Convert.ToInt32(resultTable.Rows[0]["Success"]);
                    string resultMessage = resultTable.Rows[0]["Result"]?.ToString() ?? "";
                    if (Success == 0)
                    {
                        ModelState.AddModelError("", "The stadium is already booked for another match in this timestamp");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateClubsAndStadiumsViewBags();
                return View("MatchRequest");
            }

            TempData["Success"] = "Match request submitted successfully and is awaiting admin approval.";
            return RedirectToAction(nameof(MatchRequest));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,ClubOwner")]
        public async Task<IActionResult> MatchEditList()
        {
            DataTable table;
            if (User.IsInRole("Admin"))
            {
                table = (DataTable)await _db.ExecuteDB("GET_ALL_MATCH_REQUESTS_ADMIN", returnTable: true);
            }
            else
            {
                var userIdString = _userManager.GetUserId(User);
                if (!Guid.TryParse(userIdString, out Guid userId)) return Forbid();

                SqlParameter[] param = { new SqlParameter("@User_ID", userId) };
                table = (DataTable)await _db.ExecuteDB("GET_ALL_MATCH_REQUESTS_CLUBOWNER", param, returnTable: true);
            }

            return View(table);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,ClubOwner")]
        public async Task<IActionResult> MatchRequestEdit(int id)
        {
            await PopulateClubsAndStadiumsViewBags();

            SqlParameter[] param = { new SqlParameter("@Request_ID", id) };
            DataTable matchTable = (DataTable)await _db.ExecuteDB("GET_MATCH_REQUEST_BY_ID", param, returnTable: true);

            if (matchTable == null || matchTable.Rows.Count == 0)
            {
                return NotFound();
            }

            DataRow row = matchTable.Rows[0];
            DateTime schedule = row["Schedule"] != DBNull.Value ? Convert.ToDateTime(row["Schedule"]) : DateTime.Now.AddDays(1);

            var viewModel = new MatchEditViewModel
            {
                Request_ID = row["Request_ID"] != DBNull.Value ? Convert.ToInt32(row["Request_ID"]) : 0,
                Home_Club = row["Home_Club"] != DBNull.Value ? Convert.ToInt32(row["Home_Club"]) : 0,
                Away_Club = row["Away_Club"] != DBNull.Value ? Convert.ToInt32(row["Away_Club"]) : 0,
                Stadium_ID = row["Stadium_ID"] != DBNull.Value ? Convert.ToInt32(row["Stadium_ID"]) : 0,
                MatchDate = schedule.Date,
                MatchTime = schedule.TimeOfDay,
                Note = row.Table.Columns.Contains("Add_Note") && row["Add_Note"] != DBNull.Value ? row["Add_Note"].ToString() : ""
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ClubOwner")]
        public async Task<IActionResult> MatchRequestEdit(MatchEditViewModel model)
        {
            if (model.Home_Club == model.Away_Club)
            {
                ModelState.AddModelError("", "Home and away teams cannot be the same club.");
            }

            var userIdString = _userManager.GetUserId(User);
            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                return Forbid();
            }

            DateTime schedule = model.MatchDate.Date + model.MatchTime;
            if (schedule < DateTime.Now)
            {
                ModelState.AddModelError("", "The selected match date and time must be in the future.");
            }

            DataTable? resultTable = null;

            if (ModelState.IsValid)
            {
                if (User.IsInRole("Admin"))
                {
                    SqlParameter[] editparameter = _db.CreateParams
                        (("@Request_ID", model.Request_ID),
                        ("@Home_Club", model.Home_Club),
                        ("@Away_Club", model.Away_Club),
                        ("@Stadium_ID", model.Stadium_ID),
                        ("@User_ID", userId),
                        ("@Schedule", schedule),
                        ("@Updated_Date", DateTime.Now),
                        ("@Updated_UID", userId),
                        ("@Add_Note", (object?)model.Note ?? DBNull.Value));
                    resultTable = (DataTable)await _db.ExecuteDB("EDIT_MATCH_REQUEST", editparameter, returnTable: true);
                }
                else
                {
                    SqlParameter[] insertParams = _db.CreateParams(
                        ("@Request_Type", "EditMatchRequest"),
                        ("@Home_Club", model.Home_Club),
                        ("@Away_Club", model.Away_Club),
                        ("@Stadium_ID", model.Stadium_ID),
                        ("@User_ID", userId),
                        ("@Schedule", schedule),
                        ("@Status", "Pending"),
                        ("@Requested_Date", DateTime.Now),
                        ("@Add_Note", (object?)model.Note ?? DBNull.Value),
                        ("@Temp_Req_ID", model.Temp_Req_ID));

                    resultTable = (DataTable)await _db.ExecuteDB("INSERT_MATCH_REQUEST", insertParams, returnTable: true);
                }

                if (resultTable != null && resultTable.Rows.Count > 0)
                {
                    int success = Convert.ToInt32(resultTable.Rows[0]["Success"]);
                    string resultMessage = resultTable.Rows[0]["Result"]?.ToString() ?? "";

                    if (success == 0 && resultMessage == "STADIUM_UNAVAILABLE")
                    {
                        ModelState.AddModelError("", "The selected stadium is already booked or overlaps with an existing match during this time slot.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateClubsAndStadiumsViewBags();
                return View("MatchRequestEdit", model);
            }

            TempData["Success"] = User.IsInRole("Admin")
                ? "Match updated successfully."
                : "Match edit request submitted successfully and is awaiting admin approval.";

            return RedirectToAction(nameof(MatchEditList));
        }

        private async Task PopulateClubsAndStadiumsViewBags()
        {
            var clubsTable = (DataTable)await _db.ExecuteDB("SHOW_ALL_CLUB_ADMIN", returnTable: true);
            var clubsList = new List<object>();
            if (clubsTable != null)
            {
                foreach (DataRow row in clubsTable.Rows)
                {
                    clubsList.Add(new
                    {
                        Club_ID = Convert.ToInt32(row["Club_ID"]),
                        Club_Name = row["Club_Name"]?.ToString() ?? "Unknown Club",
                        Logo_Url = row["Logo_Url"] != DBNull.Value ? row["Logo_Url"].ToString() : "/images/placeholder-club.png",
                        Location = row.Table.Columns.Contains("Location") && row["Location"] != DBNull.Value ? row["Location"].ToString() : "Phnom Penh, Cambodia"
                    });
                }
            }
            ViewBag.Clubs = clubsList;

            var stadiumsTable = (DataTable)await _db.ExecuteDB("SHOW_ALL_STADIUM_ADMIN", returnTable: true);
            var stadiumsList = new List<object>();
            if (stadiumsTable != null)
            {
                foreach (DataRow row in stadiumsTable.Rows)
                {
                    stadiumsList.Add(new
                    {
                        Stadium_ID = Convert.ToInt32(row["Stadium_ID"]),
                        Stadium_Name = row["Stadium_Name"]?.ToString() ?? "Unknown Stadium",
                        Stadium_IMG = row["Stadium_IMG"] != DBNull.Value ? row["Stadium_IMG"].ToString() : "/images/default-stadium.png",
                        Location = row.Table.Columns.Contains("Stadium_Location") && row["Stadium_Location"] != DBNull.Value ? row["Stadium_Location"].ToString() : "Phnom Penh, Cambodia"
                    });
                }
            }
            ViewBag.Stadiums = stadiumsList;
        }
    }
}