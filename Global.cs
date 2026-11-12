using _bk12.Data;
using System.Data;
using QRCoder;

namespace _bk12
{
    public static class Global
    {
        public static Club MapClubFromRow(DataRow row)
        {
            return new Club
            {
                Club_ID = Convert.ToInt32(row["Club_ID"]),
                User_ID = row["User_ID"] != DBNull.Value ? (Guid?)Guid.Parse(row["User_ID"].ToString()!) : null,
                Club_Name = row["Club_Name"] as string ?? string.Empty,
                Logo_Url = row["Logo_Url"] as string,
                Description = row["Description"] as string,
                Inactive = Convert.ToBoolean(row["Inactive"]),
                Created_Date = Convert.ToDateTime(row["Created_Date"]),
                Updated_UID = row["Updated_UID"] != DBNull.Value ? (Guid?)Guid.Parse(row["Updated_UID"].ToString()!) : null,
                Updated_Date = (DateTime)(row["Updated_Date"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["Updated_Date"]) : null)
            };
        }
        public static byte[] GenerateQr(Guid Ticket_UID)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodedata = qrGenerator.CreateQrCode(
                Ticket_UID.ToString(),
                QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodedata);

            return qrCode.GetGraphic(20);
        }
    }
}
