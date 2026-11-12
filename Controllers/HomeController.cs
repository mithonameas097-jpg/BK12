using _bk12.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Diagnostics;

namespace _bk12.Controllers
{
    public class HomeController : Controller
    {
        private readonly Connection _db;
        public HomeController(Connection db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var matchTable = (DataTable)await _db.ExecuteDB("SHOW_MATCH_HOME", null, returnTable: true);
            var matches = new List<_bk12.Models.HomeMatchViewModel>();

            if (matchTable != null)
            {
                foreach (DataRow row in matchTable.Rows)
                {
                    matches.Add(new _bk12.Models.HomeMatchViewModel
                    {
                        Match_ID = Convert.ToInt32(row["Match_ID"]),
                        Schedule = Convert.ToDateTime(row["Schedule"]),
                        Status = row["Status"]?.ToString() ?? "Upcoming",
                        Home_Club_Name = row["Home_Club_Name"]?.ToString() ?? "Home",
                        Home_Club_Logo = row["Home_Club_Logo"]?.ToString() ?? "/images/default-club.png",
                        Away_Club_Name = row["Away_Club_Name"]?.ToString() ?? "Away",
                        Away_Club_Logo = row["Away_Club_Logo"]?.ToString() ?? "/images/default-club.png",
                        Stadium_Name = row["Stadium_Name"]?.ToString() ?? "Stadium",
                        Map_Url = row["Map_Url"]?.ToString() ?? "#"
                    });
                }
            }

            return View(matches);
        }
        [Authorize(Roles = "User")]
        public IActionResult Privacy()
        {
            return View();
        }
        [Authorize]
        public IActionResult About() { return View(); }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult Contact()
        {
            return View();
        }
    }
}
