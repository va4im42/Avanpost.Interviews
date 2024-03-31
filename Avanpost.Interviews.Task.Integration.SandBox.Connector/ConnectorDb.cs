using Avanpost.Interviews.Task.Integration.Data.Models;
using Avanpost.Interviews.Task.Integration.Data.Models.Models;
using Avanpost.Interviews.Task.Integration.SandBox.Connector.Models;
using Microsoft.EntityFrameworkCore;
using System; 
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;

namespace Avanpost.Interviews.Task.Integration.SandBox.Connector
{
    public class ConnectorDb : IConnector
    {

        public ConnectorDb() { }

        private TestDbContext _testDbContext;
        
        private const string _templatePermission = @"\w+:\d+";

        public void StartUp(string connectionString)
        {
            DbContextOptionsBuilder<TestDbContext> optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
            DbContextOptions<TestDbContext> options;
            DbConnectionStringBuilder connectionStringBuilder = new DbConnectionStringBuilder();
            connectionStringBuilder.ConnectionString = connectionString;

            try
            {
                string conectString = connectionStringBuilder["ConnectionString"] as string ?? throw new Exception("The connection string cannot be found.");
                string providerString = connectionStringBuilder["Provider"] as string ?? throw new Exception("The provider string cannot be found.");

                if (providerString.Contains("PostgreSQL")) options = optionsBuilder.UseNpgsql(conectString).Options;
                else if (providerString.Contains("SqlServer")) options = optionsBuilder.UseSqlServer(conectString).Options;
                else throw new Exception("Provider ne podderzhivaets'ya");
                _testDbContext = new TestDbContext(options);
            }
            catch(Exception ex) 
            {
                Logger.Error(ex.Message);
                throw;
            }
            
        }

        public void CreateUser(UserToCreate user)
        {
            try
            {
                User newUser = new User
                {
                    Login = user.Login,
                    LastName = user.Properties.SingleOrDefault(x => x.Name == nameof(newUser.LastName))?.Value ?? string.Empty,
                    FirstName = user.Properties.SingleOrDefault(x => x.Name == nameof(newUser.FirstName))?.Value ?? string.Empty,
                    MiddleName = user.Properties.SingleOrDefault(x => x.Name == nameof(newUser.MiddleName))?.Value ?? string.Empty,
                    TelephoneNumber = user.Properties.SingleOrDefault(x => x.Name == nameof(newUser.TelephoneNumber))?.Value ?? string.Empty,
                    IsLead = Convert.ToBoolean(user.Properties.SingleOrDefault(x => x.Name == nameof(newUser.IsLead))?.Value)

                };
                
                _testDbContext.Users.Add(newUser);
                Password newPassword = new Password
                {
                    UserId = user.Login,
                    UserPassword = user.HashPassword
                };

                _testDbContext.Passwords.Add(newPassword);
                _testDbContext.SaveChanges();

            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                throw;
            }
        }

        public IEnumerable<Property> GetAllProperties() => new[]
        {
            _testDbContext.Users.EntityType.GetProperties().Where(x => !x.IsKey()).Select(x => new Property(x.Name, string.Empty)),
            _testDbContext.Passwords.EntityType.GetProperties().Where(x => x.Name == "password").Select(x => new Property(x.Name, string.Empty))
        }.SelectMany(x => x);
       

        public IEnumerable<UserProperty> GetUserProperties(string userLogin)
        {
            try
            {
                User user = _testDbContext.Users.Find(userLogin) ?? throw new Exception("User don't find");

                return _testDbContext.Entry(user).Properties.Select(x => new UserProperty(x.Metadata.Name, x.CurrentValue?.ToString() ?? string.Empty));
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                throw;
            }
        }

        public bool IsUserExists(string userLogin) => _testDbContext.Users.Any(x => x.Login == userLogin);


        public void UpdateUserProperties(IEnumerable<UserProperty> properties, string userLogin)
        {
            try
            {
                User user = _testDbContext.Users.Find(userLogin) ?? throw new Exception("User don't find");
                foreach(UserProperty userProperty in properties)
                {
                    if (userProperty.Name == "password") _testDbContext.Passwords.Single(x => x.UserId == userLogin).UserPassword = userProperty.Value;
                    _testDbContext.Entry(user).Property(userProperty.Name).CurrentValue = userProperty.Value;
                }
                _testDbContext.SaveChanges();
            }

            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                throw;
            }


        }

        public IEnumerable<Permission> GetAllPermissions() => new[]
        {
            _testDbContext.RequestRights.Select(x => new Permission(x.Id.ToString(), x.Name, string.Empty)),
            _testDbContext.ItRoles.Select(x => new Permission(x.Id.ToString(), x.Name, string.Empty))

        }.SelectMany(x => x);
       

        public void AddUserPermissions(string userLogin, IEnumerable<string> rightIds)
        {
            try
            {
                foreach(string permission in rightIds)
                {
                    if (!IsExpressionRight(permission, _templatePermission)) throw new Exception("Correct format \"Permission type*:*Permision ID\"");
                    string permissionType = permission.Split(':')[0];
                    int permissionId = int.Parse(permission.Split(":")[1]);
                    if (permissionType.Equals("Role")) _testDbContext.UserItroles.Add(new UserItrole { UserId = userLogin, RoleId = permissionId });
                    else if (permissionType.Equals("Request")) _testDbContext.UserRequestRights.Add(new UserRequestRight { UserId = userLogin, RightId = permissionId });
                    else throw new Exception("Unsupported permission type");
                }
                _testDbContext.SaveChanges();
            }

            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                throw;
            }
        }

        public void RemoveUserPermissions(string userLogin, IEnumerable<string> rightIds)
        {
            try
            {
                foreach (string permission in rightIds)
                {
                    if (!IsExpressionRight(permission, _templatePermission)) throw new Exception("Correct format \"Permission type*:*Permision ID\"");
                    string permissionType = permission.Split(':')[0];
                    int permissionId = int.Parse(permission.Split(":")[1]);
                    if (permissionType.Equals("Role")) 
                        _testDbContext.UserItroles.Remove(_testDbContext.UserItroles.First(x => x.UserId == userLogin && x.RoleId == permissionId));
                    else if (permissionType.Equals("Request")) 
                        _testDbContext.UserRequestRights.Remove(_testDbContext.UserRequestRights.First(x => x.UserId == userLogin && x.RightId == permissionId));
                    else throw new Exception("Unsupported permission type");
                }
                _testDbContext.SaveChanges();
            }

            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                throw;
            }
        }

        public IEnumerable<string> GetUserPermissions(string userLogin) => new[] 
        {
            _testDbContext.UserItroles.Where(x => x.UserId == userLogin).Join(_testDbContext.ItRoles, outer => outer.RoleId, inner => inner.Id, (outer,  inner) => inner.Name),

            _testDbContext.UserRequestRights.Where(x => x.UserId == userLogin).Join(_testDbContext.RequestRights, outer => outer.RightId, inner => inner.Id, (outer, inner) => inner.Name)
        
        
        }.SelectMany(x => x);


        public ILogger Logger { get; set; }

        private bool IsExpressionRight(string expression, string template)
        {
            Regex regex = new Regex(template);
            return regex.IsMatch(expression);
        }
    }
}


