using MetaFrm.Stock.Console;
using Microsoft.AspNetCore.Components.Authorization;

namespace MetaFrm.Stock.Exchange
{
    /// <summary>
    /// Exchanger
    /// </summary>
    public class Exchanger : ICore
    {
        /// <summary>
        /// AuthState
        /// </summary>
        private Task<AuthenticationState> AuthState { get; set; }

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
        /// <param name="authState"></param>
        /// <param name="exchangeID"></param>
        public Exchanger(Task<AuthenticationState> authState, int exchangeID)
        { 
            this.AuthState = authState;
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
            User? user;

            lock (this.Users)
            {
                user = this.Users.SingleOrDefault(x => x.UserID == userID);

                if (user != null)
                    return user;

                user = new(this.AuthState)
                {
                    ExchangeID = this.ExchangeID,
                    UserID = userID,
                    Api = CreateApi()
                };

                if (user.Api == null)
                    return null;

                user.Api.AccessKey = accessKey;
                user.Api.SecretKey = secretKey;

                user.IsFirstUser = (this.Users.Count == 0);
                this.Users.Add(user);
            }

            user.Start();
            $"Added User".WriteMessage(this.ExchangeID, user.UserID);

            return user;
        }
        /// <summary>
        /// CreateApi
        /// </summary>
        /// <returns></returns>
        public IApi? CreateApi()
        {
            lock (this.Users)
                return CreateApi(this.ExchangeID, this.Users.Count, true);
        }
        /// <summary>
        /// CreateApi
        /// </summary>
        /// <param name="exchangeID"></param>
        /// <param name="userCount"></param>
        /// <param name="runOrderResultFromWebSocket"></param>
        /// <returns></returns>
        public static IApi? CreateApi(int exchangeID, int userCount, bool runOrderResultFromWebSocket)
        {
            return exchangeID switch
            {
                1 => new Stock.Exchange.Upbit.UpbitApi(userCount == 0, runOrderResultFromWebSocket),
                2 => new Stock.Exchange.Bithumb.BithumbApi(userCount == 0, runOrderResultFromWebSocket),
                _ => null
            };
        }

        /// <summary>
        /// RemoveUser
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="saveWorkDataList"></param>
        /// <returns></returns>
        public async Task<bool> RemoveUser(int userID, bool saveWorkDataList)
        {
            var sel = this.Users.SingleOrDefault(x => x.UserID == userID);

            if (sel != null)
                return await this.RemoveUser(sel, saveWorkDataList  );

            return false;
        }
        /// <summary>
        /// RemoveUser
        /// </summary>
        /// <param name="user"></param>
        /// <param name="saveWorkDataList"></param>
        /// <returns></returns>
        public async Task<bool> RemoveUser(User user, bool saveWorkDataList)
        {
            if (this.Users.Contains(user))
            {
                user.SaveWorkDataList = saveWorkDataList;
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
        /// <param name="saveWorkDataList"></param>
        /// <returns></returns>
        public async Task<bool> Exit(bool saveWorkDataList)
        {
            List<User> users = new();

            lock (this.Users)
                foreach (var user in this.Users)
                    users.Add(user);

            foreach (var user in users)
                if (this.RemoveUser(user, saveWorkDataList).Result)
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