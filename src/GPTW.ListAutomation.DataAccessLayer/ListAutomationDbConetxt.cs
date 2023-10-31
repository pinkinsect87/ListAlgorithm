using GPTW.ListAutomation.DataAccessLayer.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace GPTW.ListAutomation.DataAccessLayer
{
    public interface IDbContext : IDisposable
    {
        /// <summary>
        /// Creates a DbSet that can be used to query and save instances of entity
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <returns>A set for the given entity type</returns>
        DbSet<T> Set<T>() where T : class;

        /// <summary>
        /// Saves the changes.
        /// </summary>
        /// <returns></returns>
        int SaveChanges();

        /// <summary>
        /// Saves the changes.
        /// </summary>
        /// <returns></returns>
        Task<int> SaveChangesAsync();

        /// <summary>
        /// Detach an entity from the context
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entity">Entity</param>
        void Detach<T>(T entity) where T : class;

        /// <summary>
        /// Detach
        /// </summary>
        /// <param name="entity"></param>
        void Detach(object entity);

        /// <summary>
        /// ReloadEntity
        /// </summary>
        /// <param name="entity"></param>
        void ReloadEntity(object entity);

        /// <summary>
        /// Gets the Database.
        /// </summary>
        /// <returns></returns>
        DatabaseFacade Database { get; }

        /// <summary>
        /// Create Database Transaction
        /// </summary>
        /// <returns>IDbContextTransaction to be used.</returns>
        IDbContextTransaction CreateTransaction();

        /// <summary>
        /// ChangeTracker
        /// </summary>
        ChangeTracker ChangeTracker { get; }

        /// <summary>
        /// Model
        /// </summary>
        IModel Model { get; }
    }

    public partial class ListAutomationDbConetxt : DbContext, IDbContext
    {
        #region Ctor.

        public ListAutomationDbConetxt(DbContextOptions<ListAutomationDbConetxt> options) : base(options) { }

        #endregion

        #region Methods

        public new DbSet<T> Set<T>() where T : class
        {
            return base.Set<T>();
        }

        public IDbContextTransaction CreateTransaction()
        {
            return base.Database.BeginTransaction();
        }

        public void Detach(object entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            this.Detach(entity);
        }

        public void Detach<T>(T entity) where T : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            this.Detach(entity);
        }

        public override int SaveChanges()
        {
            return base.SaveChanges();
        }

        public Task<int> SaveChangesAsync()
        {
            return base.SaveChangesAsync();
        }

        public void ReloadEntity(object entity)
        {
            this.Entry(entity).Reload();
        }

        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var assignTypeFrom = typeof(IMappingConfiguration);
            var typeConfigurations = this.GetType().Assembly
                .GetTypes()
                .Where(t => assignTypeFrom.IsAssignableFrom(t) && !t.IsInterface && !t.IsGenericType).ToList();

            foreach (Type typeConfiguration in typeConfigurations)
            {
                var configuration = Activator.CreateInstance(typeConfiguration) as IMappingConfiguration;
                if (configuration != null) configuration.ApplyConfiguration(modelBuilder);
            }

            base.OnModelCreating(modelBuilder);
        }
    }
}
