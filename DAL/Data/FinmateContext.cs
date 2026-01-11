using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Data
{
    public class FinmateContext : DbContext
    {
        // Constructor cho DI (Dependency Injection)
        public FinmateContext(DbContextOptions<FinmateContext> options) : base(options)
        {
        }

        // Constructor mặc định cho testing/migration
        public FinmateContext()
        {
        }

        //========================================================================================================================
        //Khai báo entity 
        //Dbset biểu diễn 1 bảng của csdl 
        public DbSet<Users> Users { get; set; }



        //Nếu muốn cấu hình chi tiết thêm thì overrive OnModelCreating
        //Nếu đã sử dụng [] trc các attribute thì có thể ko cần method này 
        //Nếu có 1 số cái phức tạp mà [] ko thể triển khai hết thì nên dùng method này 
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Setup lại các thuộc tính cho entity user , đã có [] vẫn có thể settup đc nó sẽ override
            modelBuilder.Entity<Users>(entity =>
            {

            });
        }

    }
}
