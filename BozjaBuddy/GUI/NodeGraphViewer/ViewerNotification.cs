using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    public class ViewerNotificationManager
    {
        public const int EXPIRE_CHECK_INTERVAL = 300;
        private Dictionary<string, ViewerNotification> notifications = new();
        private DateTime _lastExpireCheckTime = DateTime.Now;
        private List<ViewerNotification> _cachedSortedNotifs = new();
        private bool _needSorting = true;

        public ViewerNotificationManager() { }

        /// <summary>Get a list of on-going notifications, sorted by their last-pushed time.</summary>
        public List<ViewerNotification> GetNotifications(bool sortByNewest = true)
        {
            this.Refresh(sortByNewest);
            return this._cachedSortedNotifs;
        }
        /// <summary>
        /// Add a new notification. 
        /// If there's already a notification with the same id, said notification is renewed and this method returns false. Otherwise true.
        /// </summary>
        public bool Push(ViewerNotification notification)
        {
            if (this.notifications.TryGetValue(notification.id, out var n))
            {
                n.Renew();
                PluginLog.LogDebug($"> Noti renewed. Current noti = {this.notifications.Count}");
                this._needSorting = true;
                return false;
            }
            this.notifications.Add(notification.id, notification);
            this._needSorting = true;
            PluginLog.LogDebug($"> Noti pushed. Current noti = {this.notifications.Count}");
            return true;
        }
        /// <summary>
        /// Add new notifications, or renew existing ones. Returns true if any new ones is added, otherwise false.
        /// </summary>
        public bool Push(List<ViewerNotification> notiListener)
        {
            if (notiListener.Count == 0) return false;

            bool tRes = false;
            foreach (var notification in notiListener)
            {
                if (this.Push(notification)) tRes = true;
            }
            return tRes;
        }
        /// <summary>
        /// <para>Remove expired notifs, and cache sorted notifs list if possible.</para>
        /// Returns true if notifications list needs sorting, otherwise false.
        /// <para>Has a cooldown, but realistically, </para>
        /// </summary>
        private void Refresh(bool sortByNewest = true)
        {
            // sorting
            if (this._needSorting)
            {
                if (this.notifications.Count < 2)
                {
                    this._cachedSortedNotifs = this.notifications.Values.ToList();
                    this._needSorting = false;
                }
                else
                {
                    this._cachedSortedNotifs = sortByNewest
                                                ? this.notifications.Values.OrderByDescending(o => o.sendTime).ToList()
                                                : this.notifications.Values.OrderBy(o => o.sendTime).ToList();
                    this._needSorting = false;
                }
            }

            // check expire
            if ((DateTime.Now - this._lastExpireCheckTime).TotalMilliseconds > ViewerNotificationManager.EXPIRE_CHECK_INTERVAL)
            {
                foreach (var n in this._cachedSortedNotifs.Where(n => n.IsExpired()).ToList())
                {
                    this.notifications.Remove(n.id);
                    this._needSorting = true;
                }
                this._lastExpireCheckTime = DateTime.Now;
            }
        }
    }
    public class ViewerNotification
    {
        public const float DURATION_DEFAULT = 4000;

        public string id;
        public ViewerNotificationType type;
        public string content;
        public DateTime sendTime { get; private set; }
        public readonly float duration;
        public Vector2? contentImguiSize = null;

        private ViewerNotification() { }
        public ViewerNotification(string id, string content, ViewerNotificationType type = ViewerNotificationType.Info, float duration = ViewerNotification.DURATION_DEFAULT)
        {
            this.id = id;
            this.type = type;
            this.content = content;
            this.duration = duration;
            this.sendTime = DateTime.Now;
        }
        public void Renew() => this.sendTime = DateTime.Now;
        public bool IsExpired() => (DateTime.Now - this.sendTime).TotalMilliseconds > this.duration;
        public double GetTimeLeft() => (this.duration - (DateTime.Now - this.sendTime).TotalMilliseconds) < 0 ? 0 : (this.duration - (DateTime.Now - this.sendTime).TotalMilliseconds);
    }
    public enum ViewerNotificationType
    {
        None = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }
}
