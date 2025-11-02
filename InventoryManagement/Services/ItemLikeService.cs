using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Services
{
    public class ItemLikeService
    {
        private readonly ApplicationDbContext _db;
        public ItemLikeService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<(bool liked, int count)> ToggleLikeByItemGuidAsync(Guid itemGuid, string userId)
        {
            var itemId = await _db.Items
                .Where(i => i.Guid == itemGuid)
                .Select(i => i.Id)
                .FirstOrDefaultAsync();
            if (itemId == 0)
            {
                return (false, 0);
            }

            var existing = await _db.ItemLikes
                .FirstOrDefaultAsync(l => EF.Property<int>(l, "ItemId") == itemId && EF.Property<string>(l, "UserId") == userId);

            bool liked;
            if (existing == null)
            {
                var itemRef = new Models.Item { Id = itemId };
                _db.Attach(itemRef);
                var userRef = new Models.AppUser { Id = userId };
                _db.Attach(userRef);
                _db.ItemLikes.Add(new Models.ItemLike { Item = itemRef, User = userRef });
                liked = true;
            }
            else
            {
                _db.ItemLikes.Remove(existing);
                liked = false;
            }

            await _db.SaveChangesAsync();
            var count = await _db.ItemLikes.CountAsync(l => EF.Property<int>(l, "ItemId") == itemId);
            return (liked, count);
        }
    }
}
