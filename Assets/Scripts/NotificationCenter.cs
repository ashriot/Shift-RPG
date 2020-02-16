using System;
using System.Collections.Generic;
using UnityEngine;
using SenderTable = System.Collections.Generic.Dictionary<System.Object, System.Collections.Generic.List<System.EventHandler>>;

public class NotificationCenter {

  public readonly static NotificationCenter instance = new NotificationCenter();

  private Dictionary<string, SenderTable> _table = new Dictionary<string, SenderTable>();
  
  private NotificationCenter() {}

  private SenderTable GetSenderTable(string notificationName) {
    if (!_table.ContainsKey(notificationName)) {
      _table.Add(notificationName, new SenderTable());
    }
    return _table[notificationName];
  }

  private List<EventHandler> GetObservers(SenderTable subTable, System.Object sender) {
    if (!subTable.ContainsKey(sender)) {
      subTable.Add(sender, new List<EventHandler>());
    }
    return subTable[sender];
  }

  public void AddObserver(EventHandler handler, string notificationName) {
    AddObserver(handler, notificationName, null);
  }

  public void AddObserver(EventHandler handler, string notificationName, System.Object sender) {
    if (handler is null) {
      Debug.LogError("Can't add a null event handler for notification");
      return;
    }

    if (string.IsNullOrEmpty(notificationName)) {
      Debug.LogError("Can't observe an unnamed notification!");
      return;
    }

    var subTable = GetSenderTable(notificationName);
    var key = sender ?? this;
    var list = GetObservers(subTable, key);
    if (!list.Contains(handler)) {
      list.Add(handler);
    }
  }

  public void RemoveObserver(EventHandler handler) {
    var keys = new string[_table.Keys.Count];
    _table.Keys.CopyTo(keys, 0);
    for (var i = keys.Length - 1; i >= 0; --i) {
      RemoveObserver(handler, keys[i]);
    }
  }

  public void RemoveObserver(EventHandler handler, string notificationName) {
    if (handler is null) {
      Debug.LogError("Can't remove a null event handler from notification!");
      return;
    }

    if (string.IsNullOrEmpty(notificationName)) {
      Debug.LogError("A notification name is required to stop observation!");
      return;
    }

    // No need to take action if we don't monitor this notification
    if (!_table.ContainsKey(notificationName)) {
      return;
    }

    var keys = new object[_table[notificationName].Keys.Count];
    _table[notificationName].Keys.CopyTo(keys, 0);
    for (var i = keys.Length - 1; i >= 0; --i) {
      RemoveObserver(handler, notificationName, keys);
    }
  }

  public void RemoveObserver(EventHandler handler, string notificationName, object sender) {
    if (string.IsNullOrEmpty(notificationName)) {
      Debug.LogError("A notification name is required to stop observation!");
      return;
    }

    // No need to take action if we don't monitor this notification
    if (!_table.ContainsKey(notificationName)) {
      return;
    }

    var subTable = GetSenderTable(notificationName);
    var key = sender ?? this;
    if (!subTable.ContainsKey(key)) {
      return;
    }
    var list = GetObservers(subTable, key);

    for (var i = list.Count - 1; i >= 0; --i) {
      if (list[i] == handler) {
        list.RemoveAt(i);
        break;
      }
    }
    if (list.Count == 0) {
      subTable.Remove(key);
      if (subTable.Count == 0) {
        _table.Remove(notificationName);
      }
    }
  }

  public void PostNotification(string notificationName) {
    PostNotification(notificationName, null);
  }

  public void PostNotification(string notificationName, object sender) {
    PostNotification(notificationName, sender, EventArgs.Empty);
  }

  public void PostNotification(string notificationName, object sender, EventArgs e) {
    if (string.IsNullOrEmpty(notificationName)) {
      Debug.LogError("A notification name is required to stop observation!");
      return;
    }

    // No need to take action if we don't monitor this notification
    if (!_table.ContainsKey(notificationName)) {
      return;
    }

    // Post to subscribers who specified a sender to observe
    var subTable = GetSenderTable(notificationName);
    if (sender != null && subTable.ContainsKey(sender)) {
      var handlers = GetObservers(subTable, sender);
      for (var i = handlers.Count - 1; i >= 0; --i) {
        handlers[i](sender, e);
      }
    }

    // Post to subscribers who did NOT specify a sender to observe
    if (subTable.ContainsKey(this)) {
      var handlers = GetObservers(subTable, this);
      for (var i = handlers.Count - 1; i >= 0; --i) {
        handlers[i](sender, e);
      }
    }

  }

}
