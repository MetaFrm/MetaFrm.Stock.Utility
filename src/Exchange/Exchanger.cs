using MetaFrm.Stock.Console;

namespace MetaFrm.Stock
{
    /// <summary>
    /// Exchanger
    /// </summary>
    public class Exchanger : ICore
    {
        /// <summary>
        /// ExchangeID
        /// </summary>
        public int ExchangeID { get; set; }
        /// <summary>
        /// Users
        /// </summary>
        public List<User> Users { get; set; } = new();

        /// <summary>
        /// Exchange
        /// </summary>
        /// <param name="exchangeID"></param>
        public Exchanger(int exchangeID)
        { 
            this.ExchangeID = exchangeID;
        }

        /// <summary>
        /// AddUser
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="accessKey"></param>
        /// <param name="secretKey"></param>
        /// <returns></returns>
        public User? AddUser(int userID, string accessKey, string secretKey)
        {
            User? user = this.Users.SingleOrDefault(x => x.UserID == userID);

            if (user != null)
                return user;

            user = new()
            {
                ExchangeID = this.ExchangeID,
                UserID = userID,
                Api = this.ExchangeID switch
                {
                    1 => new Stock.Exchange.Upbit.UpbitApi(true, true),
                    2 => new Stock.Exchange.Bithumb.BithumbApi(true, false),
                    _ => null
                }
            };

            if (user.Api == null)
                return null;

            user.Api.AccessKey = accessKey;
            user.Api.SecretKey = secretKey;

            lock (Users)
            {
                user.IsFirstUser = (this.Users.Count == 0);
                this.Users.Add(user);
            }

            user.Start();
            $"Added User".WriteMessage(this.ExchangeID, user.UserID);

            return user;
        }

        /// <summary>
        /// RemoveUser
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public async Task<bool> RemoveUser(int userID)
        {
            var sel = this.Users.SingleOrDefault(x => x.UserID == userID);

            if (sel != null)
                return await this.RemoveUser(sel);

            return false;
        }
        /// <summary>
        /// RemoveUser
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<bool> RemoveUser(User user)
        {
            if (this.Users.Contains(user))
            {
                user.IsStopped = true;

                while (true)
                {
                    await Task.Delay(1000);

                    lock (user.Settings)
                        if (user.Settings.Count != 0)
                            continue;

                    break;
                }

                lock (Users)
                {
                    if (!user.IsFirstUser)
                    {
                        ((IDisposable?)user.Api)?.Dispose();
                        $"Removed User".WriteMessage(this.ExchangeID, user.UserID);
                        return this.Users.Remove(user);
                    }
                    else
                    {
                        $"FirstUser !!".WriteMessage(this.ExchangeID, user.UserID);
                        return false;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Exit
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Exit()
        {
            List<User> users = new();

            lock (this.Users)
                foreach (var user in this.Users)
                    users.Add(user);

            foreach (var user in users)
                if (this.RemoveUser(user).Result)
                    continue;

            while (true)
            {
                await Task.Delay(2000);

                lock (this.Users)
                {
                    if (this.Users.Count == 1)
                        lock (this.Users[0].Settings)
                            if (this.Users[0].Settings.Count == 0)
                            {
                                ((IDisposable?)this.Users[0].Api)?.Dispose();
                                $"Removed User".WriteMessage(this.ExchangeID, this.Users[0].UserID);
                                this.Users.Remove(this.Users[0]);

                                return true;
                            }

                    if (this.Users.Count == 0)
                        return true;
                }
            }
        }
    }
}