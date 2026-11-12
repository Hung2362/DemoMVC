using DemoMVC.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoMVC.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261112135500_AddQuanlymonhoc")]
    public partial class AddQuanlymonhoc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quanlymonhocs",
                columns: table => new
                {
                    MaMH = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenMH = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SoTinChi = table.Column<int>(type: "int", nullable: false),
                    KhoaPhuTrach = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quanlymonhocs", x => x.MaMH);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Quanlymonhocs");
        }
    }
}
