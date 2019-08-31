using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using OnlineJudgeApi.Entities;
using OnlineJudgeApi.Helpers;

namespace OnlineJudgeApi.Services
{
    public interface IUserService
    {
        User Authenticate(string username, string password);
        IEnumerable<User> GetAll();
        User GetById(int id);
        User Create(User user, string password);
        void Update(User user, string password = null);
        void Delete(int id);
    }

    public class UserService : IUserService
    {
        private DataContext context;

        public UserService(DataContext context)
        {
            this.context = context;
        }

        public User Authenticate(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return null;
            }

            var user = context.Users.SingleOrDefault(x => x.Username == username);

            // check if username exists
            if (user == null)
            {
                return null;
            }

            byte[] pwhash = Convert.FromBase64String(user.PasswordHash);
            byte[] pwsalt = Convert.FromBase64String(user.PasswordSalt);

            // check if password is correct
            if (!VerifyPasswordHash(password, pwhash, pwsalt))
            {
                return null;
            }

            // authentication successful
            return user;
        }

        public IEnumerable<User> GetAll()
        {
            return context.Users;
        }

        public User GetById(int id)
        {
            return context.Users.Find(id);
        }

        public User Create(User user, string password)
        {
            // validation
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new AppException("Password is required");
            }

            if (user.Email == null || !(new EmailAddressAttribute().IsValid(user.Email)))
            {
                throw new AppException("Invalid email address");
            }

            if (context.Users.Any(x => x.Username == user.Username || x.Email == user.Email))
            {
                throw new AppException("Username or email already taken");
            }

            CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

            user.PasswordHash = Convert.ToBase64String(passwordHash);
            user.PasswordSalt = Convert.ToBase64String(passwordSalt);

            context.Users.Add(user);
            context.SaveChanges();

            return user;
        }

        public void Update(User userParam, string password = null)
        {
            var user = context.Users.Find(userParam.Id);

            if (user == null)
            {
                throw new AppException("User not found");
            }

            if (userParam.Email != user.Email)
            {
                // username has changed so check if the new username is already taken
                if (context.Users.Any(x => x.Email == userParam.Email))
                {
                    throw new AppException("New email " + userParam.Email + " is already taken");
                }
            }

            // update user properties
            user.Email = userParam.Email;

            // update password if it was entered
            if (!string.IsNullOrWhiteSpace(password))
            {
                CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

                user.PasswordHash = Convert.ToBase64String(passwordHash);
                user.PasswordSalt = Convert.ToBase64String(passwordSalt);
            }

            context.Users.Update(user);
            context.SaveChanges();
        }

        public void Delete(int id)
        {
            var user = context.Users.Find(id);
            if (user != null)
            {
                context.Users.Remove(user);
                context.SaveChanges();
            }
        }

        // private helper methods

        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
            }

            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
            }

            if (storedHash.Length != 64)
            {
                throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");
            }

            if (storedSalt.Length != 128)
            {
                throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "passwordHash");
            }

            using (var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != storedHash[i])
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
