$(document).ready(function () {

    console.log("Clubs page loaded");

    loadClubs();

});

function loadClubs() {

    $.ajax({
        url: '/Clubs/GetClubs',
        type: 'GET',

        success: function (clubs) {

            let html = "";

            if (isAdmin) {

                html += `
                    <div class="mb-3">
                        <button class="btn btn-primary" id="btnAddClub">
                            + Add Club
                        </button>
                    </div>
                `;

            }

            if (isClubOwner) {
                if (clubs.length === 0) {
                    html += `
                            <div style="text-align: center; padding: 40px 20px;">
                                <p style="color: #fff; font-size: 18px; margin-bottom: 20px;">
                                    No club found.
                                </p>
                                <a href="/Clubs/ClubRequest"
                                   style="
                                       display: inline-block;
                                       padding: 12px 32px;
                                       background-color: #050A2E;
                                       color: #E3A649;
                                       border: 2px solid #E3A649;
                                       border-radius: 8px;
                                       font-size: 16px;
                                       font-weight: 600;
                                       letter-spacing: 0.5px;
                                       text-decoration: none;
                                       transition: all 0.3s ease;
                                       box-shadow: 0 4px 12px rgba(227, 166, 73, 0.25);
                                   "
                                   onmouseover="this.style.backgroundColor='#E3A649'; this.style.color='#050A2E'; this.style.transform='translateY(-2px)';"
                                   onmouseout="this.style.backgroundColor='#050A2E'; this.style.color='#E3A649'; this.style.transform='translateY(0)';">
                                    Request a Club
                                </a>
                            </div>`;
                } else {
                    let club = clubs[0];

                    let statusBadgeClass = club.inactive ? "bg-secondary text-white" : "bg-success text-white";
                    let statusText = club.inactive ? "Inactive" : "Active";

                    let ownerName = club.owner_Name || 'N/A';

                    html += `
                        <div class="card text-light shadow-lg mb-4" style="background-color: #050A2E; border: 2px solid #ffc107; border-radius: 15px;">
                            <div class="card-header text-dark d-flex justify-content-between align-items-center" style="background-color: #ffc107; border-top-left-radius: 13px; border-top-right-radius: 13px;">
                                <span class="badge ${statusBadgeClass} px-3 py-2" style="font-size: 0.9rem;">${statusText}</span>
                                <h4 class="mb-0 fw-bold mx-auto">Club Information</h4>
                                <button class="btn btn-dark btn-sm rounded-circle p-2" title="Edit" style="background-color: #050A2E; border-color: #050A2E;">
                                    ✎
                                </button>
                            </div>
                            <div class="card-body p-4" style="background-color: #050A2E; border-bottom-left-radius: 15px; border-bottom-right-radius: 15px;">
                                <div class="row align-items-center mb-4">
                                    <div class="col-md-3 text-center mb-3 mb-md-0">
                                        <img src="${club.logo_URL || 'https://via.placeholder.com/120'}" alt="Club Logo" class="img-fluid rounded" style="max-height: 120px; object-fit: contain;">
                                    </div>
                                    <div class="col-md-9">
                                        <h2 class="fw-bold mb-2 text-white">${club.club_Name}</h2>
                                        <p class="mb-1 text-light"><strong>Owner :</strong> ${ownerName}</p>
                                        <p class="mb-0 text-light"><strong>Created Date :</strong> ${club.created_Date ? new Date(club.created_Date).toLocaleDateString() : 'N/A'}</p>
                                    </div>
                                </div>

                                <div class="mb-4">
                                    <h5 class="fw-bold mb-2" style="color: #ffc107;">Description:</h5>
                                    <p class="text-light" style="line-height: 1.6;">${club.description ?? "No description available."}</p>
                                </div>

                                <hr style="border-color: #ffc107; opacity: 0.5;">

                                <div class="d-flex justify-content-between text-muted small px-1">
                                    <span style="color: #a0aec0;">ID: #${club.club_ID}</span>
                                    <span style="color: #a0aec0;">Updated Date : ${club.updated_Date ? new Date(club.updated_Date).toLocaleDateString() : 'N/A'}</span>
                                </div>
                            </div>
                        </div>
                    `;
                }
            }

            if (isAdmin) {
                if (clubs.length === 0) {                                                                                                         te

                    html += `
                        <p>No clubs found.</p>
                    `;

                }
                else {

                    html += `
                        <table class="table table-bordered">
                            <thead>
                                <tr>
                                    <th>Club ID</th>
                                    <th>Club Name</th>
                                    <th>Description</th>
                                    <th>Status</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>

                            <tbody>
                    `;

                    clubs.forEach(function (club) {

                        let actions = "";

                        if (isAdmin) {

                            actions = `
                                <button class="btn btn-primary btn-sm">
                                    Edit
                                </button>

                                <button class="btn btn-danger btn-sm">
                                    Deactivate
                                </button>
                            `;

                        }

                        html += `
                            <tr>
                                <td>${club.club_ID}</td>
                                <td>${club.club_Name}</td>
                                <td>${club.description ?? ""}</td>
                                <td>
                                    ${club.inactive ? "Inactive" : "Active"}
                                </td>
                                <td>
                                    ${actions}
                                </td>
                            </tr>
                        `;

                    });

                    html += `
                            </tbody>
                        </table>
                    `;
                }
            }

            $("#clubContent").html(html);
        },

        error: function (xhr) {

            console.log("Error:", xhr.status);

            $("#clubContent").html(
                "<p>Unable to load clubs.</p>"
            );
        }
    });
}

function loadClubOwners() {

    $.ajax({
        url: '/Clubs/GetClubOwners',
        type: 'GET',

        success: function (owners) {

            console.log("Club owners:", owners);

        },

        error: function (xhr) {

            console.log("Error loading club owners:", xhr.status);

        }
    });

}