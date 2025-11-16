using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

namespace InventoryManagement.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomIds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Guid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomIds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    SequentialId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    SearchVector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "to_tsvector('simple', coalesce(\"FirstName\", '') || ' ' || coalesce(\"LastName\", '') || ' ' || coalesce(\"Email\", ''))", stored: true),
                    SalesforceAccountId = table.Column<string>(type: "text", nullable: true),
                    SalesforceContactId = table.Column<string>(type: "text", nullable: true),
                    SalesforceInstanceUrl = table.Column<string>(type: "text", nullable: true),
                    SalesforceAccessToken = table.Column<string>(type: "text", nullable: true),
                    SalesforceRefreshToken = table.Column<string>(type: "text", nullable: true),
                    SalesforceTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AbstractElement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeparatorBefore = table.Column<char>(type: "character(1)", nullable: true),
                    SeparatorAfter = table.Column<char>(type: "character(1)", nullable: true),
                    Position = table.Column<short>(type: "smallint", nullable: true),
                    CustomId_Id = table.Column<int>(type: "integer", nullable: false),
                    ElementType = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    Radix = table.Column<int>(type: "integer", nullable: true),
                    PaddingChar = table.Column<char>(type: "character(1)", nullable: true),
                    DateTimeFormat = table.Column<string>(type: "text", nullable: true),
                    FixedText = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbstractElement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbstractElement_CustomIds_CustomId_Id",
                        column: x => x.CustomId_Id,
                        principalTable: "CustomIds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Inventories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    Owner_UserId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SearchVector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "to_tsvector('simple', coalesce(\"Title\", '') || ' ' || coalesce(\"Description\", ''))", stored: true),
                    CustomIdId = table.Column<int>(type: "integer", nullable: false),
                    SingleLine1_Title = table.Column<string>(type: "text", nullable: true),
                    SingleLine1_Description = table.Column<string>(type: "text", nullable: true),
                    SingleLine1_Position = table.Column<short>(type: "smallint", nullable: true),
                    SingleLine1_IsUsed = table.Column<bool>(type: "boolean", nullable: true),
                    SingleLine2_Title = table.Column<string>(type: "text", nullable: true),
                    SingleLine2_Description = table.Column<string>(type: "text", nullable: true),
                    SingleLine2_Position = table.Column<short>(type: "smallint", nullable: true),
                    SingleLine2_IsUsed = table.Column<bool>(type: "boolean", nullable: true),
                    SingleLine3_Title = table.Column<string>(type: "text", nullable: true),
                    SingleLine3_Description = table.Column<string>(type: "text", nullable: true),
                    SingleLine3_Position = table.Column<short>(type: "smallint", nullable: true),
                    SingleLine3_IsUsed = table.Column<bool>(type: "boolean", nullable: true),
                    MultiLine1_Title = table.Column<string>(type: "text", nullable: true),
                    MultiLine1_Description = table.Column<string>(type: "text", nullable: true),
                    MultiLine1_Position = table.Column<short>(type: "smallint", nullable: true),
                    MultiLine1_IsUsed = table.Column<bool>(type: "boolean", nullable: true),
                    MultiLine2_Title = table.Column<string>(type: "text", nullable: true),
                    MultiLine2_Description = table.Column<string>(type: "text", nullable: true),
                    MultiLine2_Position = table.Column<short>(type: "smallint", nullable: true),
                    MultiLine2_IsUsed = table.Column<bool>(type: "boolean", nullable: true),
                    MultiLine3_Title = table.Column<string>(type: "text", nullable: true),
                    MultiLine3_Description = table.Column<string>(type: "text", nullable: true),
                    MultiLine3_Position = table.Column<short>(type: "smallint", nullable: true),
                    MultiLine3_IsUsed = table.Column<bool>(type: "boolean", nullable: true),
                    NumericLine1_Title = table.Column<string>(type: "text", nullable: true),
                    NumericLine1_Description = table.Column<string>(type: "text", nullable: true),
                    NumericLine1_Position = table.Column<short>(type: "smallint", nullable: true),
                    NumericLine1_IsUsed = table.Column<bool>(type: "boolean", nullable: true),
                    NumericLine2_Title = table.Column<string>(type: "text", nullable: true),
                    NumericLine2_Description = table.Column<string>(type: "text", nullable: true),
                    NumericLine2_Position = table.Column<short>(type: "smallint", nullable: true),
                    NumericLine2_IsUsed = table.Column<bool>(type: "boolean", nullable: true),
                    NumericLine3_Title = table.Column<string>(type: "text", nullable: true),
                    NumericLine3_Description = table.Column<string>(type: "text", nullable: true),
                    NumericLine3_Position = table.Column<short>(type: "smallint", nullable: true),
                    NumericLine3_IsUsed = table.Column<bool>(type: "boolean", nullable: true),
                    BoolLine1_Title = table.Column<string>(type: "text", nullable: true),
                    BoolLine1_Description = table.Column<string>(type: "text", nullable: true),
                    BoolLine1_Position = table.Column<short>(type: "smallint", nullable: true),
                    BoolLine1_IsUsed = table.Column<bool>(type: "boolean", nullable: true),
                    BoolLine2_Title = table.Column<string>(type: "text", nullable: true),
                    BoolLine2_Description = table.Column<string>(type: "text", nullable: true),
                    BoolLine2_Position = table.Column<short>(type: "smallint", nullable: true),
                    BoolLine2_IsUsed = table.Column<bool>(type: "boolean", nullable: true),
                    BoolLine3_Title = table.Column<string>(type: "text", nullable: true),
                    BoolLine3_Description = table.Column<string>(type: "text", nullable: true),
                    BoolLine3_Position = table.Column<short>(type: "smallint", nullable: true),
                    BoolLine3_IsUsed = table.Column<bool>(type: "boolean", nullable: true),
                    LinkLine1_Title = table.Column<string>(type: "text", nullable: true),
                    LinkLine1_Description = table.Column<string>(type: "text", nullable: true),
                    LinkLine1_Position = table.Column<short>(type: "smallint", nullable: true),
                    LinkLine1_IsUsed = table.Column<bool>(type: "boolean", nullable: true),
                    LinkLine2_Title = table.Column<string>(type: "text", nullable: true),
                    LinkLine2_Description = table.Column<string>(type: "text", nullable: true),
                    LinkLine2_Position = table.Column<short>(type: "smallint", nullable: true),
                    LinkLine2_IsUsed = table.Column<bool>(type: "boolean", nullable: true),
                    LinkLine3_Title = table.Column<string>(type: "text", nullable: true),
                    LinkLine3_Description = table.Column<string>(type: "text", nullable: true),
                    LinkLine3_Position = table.Column<short>(type: "smallint", nullable: true),
                    LinkLine3_IsUsed = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inventories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Inventories_CustomIds_CustomIdId",
                        column: x => x.CustomIdId,
                        principalTable: "CustomIds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Inventory_Owner",
                        column: x => x.Owner_UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiscussionPosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ContentMarkdown = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscussionPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscussionPost_Inventory",
                        column: x => x.InventoryId,
                        principalTable: "Inventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscussionPost_User",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryAllowedUsers",
                columns: table => new
                {
                    InventoryId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryAllowedUsers", x => new { x.InventoryId, x.UserId });
                    table.ForeignKey(
                        name: "FK_InventoryAllowedUsers_InventoryId",
                        column: x => x.InventoryId,
                        principalTable: "Inventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryAllowedUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTags",
                columns: table => new
                {
                    InventoryId = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTags", x => new { x.InventoryId, x.TagId });
                    table.ForeignKey(
                        name: "FK_InventoryTags_InventoryId",
                        column: x => x.InventoryId,
                        principalTable: "Inventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryTags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryId = table.Column<int>(type: "integer", nullable: false),
                    Owner_UserId = table.Column<string>(type: "text", nullable: true),
                    CustomId = table.Column<string>(type: "text", nullable: false),
                    SingleLine1 = table.Column<string>(type: "text", nullable: true),
                    SingleLine2 = table.Column<string>(type: "text", nullable: true),
                    SingleLine3 = table.Column<string>(type: "text", nullable: true),
                    MultiLine1 = table.Column<string>(type: "text", nullable: true),
                    MultiLine2 = table.Column<string>(type: "text", nullable: true),
                    MultiLine3 = table.Column<string>(type: "text", nullable: true),
                    NumericLine1 = table.Column<double>(type: "double precision", nullable: true),
                    NumericLine2 = table.Column<double>(type: "double precision", nullable: true),
                    NumericLine3 = table.Column<double>(type: "double precision", nullable: true),
                    BoolLine1 = table.Column<bool>(type: "boolean", nullable: true),
                    BoolLine2 = table.Column<bool>(type: "boolean", nullable: true),
                    BoolLine3 = table.Column<bool>(type: "boolean", nullable: true),
                    LinkLine1 = table.Column<string>(type: "text", nullable: true),
                    LinkLine2 = table.Column<string>(type: "text", nullable: true),
                    LinkLine3 = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Item_Inventory",
                        column: x => x.InventoryId,
                        principalTable: "Inventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Item_Owner",
                        column: x => x.Owner_UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ItemLikes",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemLikes", x => new { x.ItemId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ItemLike_Item",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemLike_User",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbstractElement_CustomId_Id",
                table: "AbstractElement",
                column: "CustomId_Id");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionPosts_InventoryId_CreatedAtUtc",
                table: "DiscussionPosts",
                columns: new[] { "InventoryId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionPosts_UserId",
                table: "DiscussionPosts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_CategoryId",
                table: "Inventories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_CustomIdId",
                table: "Inventories",
                column: "CustomIdId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_Description",
                table: "Inventories",
                column: "Description")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_Owner_UserId",
                table: "Inventories",
                column: "Owner_UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_SearchVector",
                table: "Inventories",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_Title",
                table: "Inventories",
                column: "Title")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAllowedUsers_UserId",
                table: "InventoryAllowedUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTags_InventoryId",
                table: "InventoryTags",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTags_TagId",
                table: "InventoryTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemLikes_UserId",
                table: "ItemLikes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_InventoryId_CustomId",
                table: "Items",
                columns: new[] { "InventoryId", "CustomId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_Owner_UserId",
                table: "Items",
                column: "Owner_UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_FirstName",
                table: "Users",
                column: "FirstName")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastName",
                table: "Users",
                column: "LastName")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_SearchVector",
                table: "Users",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbstractElement");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "DiscussionPosts");

            migrationBuilder.DropTable(
                name: "InventoryAllowedUsers");

            migrationBuilder.DropTable(
                name: "InventoryTags");

            migrationBuilder.DropTable(
                name: "ItemLikes");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Inventories");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "CustomIds");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
