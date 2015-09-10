﻿using System;
using System.Data;
using System.Text;
using System.Collections.Generic;
using OTA.Data.Entity.Models;
using System.Linq;
using OTA.Permissions;
using System.Threading.Tasks;

namespace OTA.Data
{
    /// <summary>
    /// Generic OTA group information
    /// </summary>
    public class Group
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public bool ApplyToGuests { get; set; }

        public string Parent { get; set; }

        public byte Chat_Red { get; set; }

        public byte Chat_Green { get; set; }

        public byte Chat_Blue { get; set; }

        public string Chat_Prefix { get; set; }

        public string Chat_Suffix { get; set; }
    }

    //    /// <summary>
    //    /// Permission node.
    //    /// </summary>
    //    public struct PermissionNode
    //    {
    //        public string Node { get; set; }
    //
    //        public bool Deny { get; set; }
    //    }

    /// <summary>
    /// The interface behind custom permissions handlers
    /// </summary>
    public interface IPermissionHandler
    {
        Permission IsPermitted(string node, BasePlayer player);

        #region "Management"

        /// <summary>
        /// Find a group by name
        /// </summary>
        /// <returns>The group.</returns>
        Group FindGroup(string name);

        /// <summary>
        /// Add the or update a group.
        /// </summary>
        /// <returns><c>true</c>, if or update group was added, <c>false</c> otherwise.</returns>
        bool AddOrUpdateGroup(string name, bool applyToGuests = false, string parent = null, byte r = 255, byte g = 255, byte b = 255, string prefix = null, string suffix = null);

        /// <summary>
        /// Remove a group from the data store.
        /// </summary>
        /// <returns><c>true</c>, if the group was removed, <c>false</c> otherwise.</returns>
        /// <param name="name">Name.</param>
        bool RemoveGroup(string name);

        /// <summary>
        /// Add a group node to the data store
        /// </summary>
        /// <returns><c>true</c>, if the group node was added, <c>false</c> otherwise.</returns>
        bool AddGroupNode(string groupName, string node, Permission permission);

        /// <summary>
        /// Remove a group node from the data store
        /// </summary>
        /// <returns><c>true</c>, if the group node was removed, <c>false</c> otherwise.</returns>
        bool RemoveGroupNode(string groupName, string node, Permission permission);

        /// <summary>
        /// Fetches the list of group names from the data store.
        /// </summary>
        string[] GroupList();

        /// <summary>
        /// Fetch the list of nodes for a group
        /// </summary>
        NodePermission[] GroupNodes(string groupName);

        /// <summary>
        /// Add a user to a group.
        /// </summary>
        /// <returns><c>true</c>, if the user was added to the group, <c>false</c> otherwise.</returns>
        bool AddUserToGroup(string username, string groupName);

        /// <summary>
        /// Remove a user from a group
        /// </summary>
        /// <returns><c>true</c>, if the user was removed from the group, <c>false</c> otherwise.</returns>
        bool RemoveUserFromGroup(string username, string groupName);

        /// <summary>
        /// Add a node to the user.
        /// </summary>
        /// <returns><c>true</c>, if the node was added to the user, <c>false</c> otherwise.</returns>
        bool AddNodeToUser(string username, string node, Permission permission);

        /// <summary>
        /// Removed a node from a user
        /// </summary>
        /// <returns><c>true</c>, if the node was removed from the user, <c>false</c> otherwise.</returns>
        bool RemoveNodeFromUser(string username, string node, Permission permission);

        /// <summary>
        /// Fetch the group names a user is associated to
        /// </summary>
        /// <remarks>Currently should always be 1</remarks>
        string[] UserGroupList(string username);

        /// <summary>
        /// Fetch the nodes a user has specific access to
        /// </summary>
        /// <returns>The nodes.</returns>
        /// <param name="username">Username.</param>
        NodePermission[] UserNodes(string username);

        /// <summary>
        /// Fetches the lowest inherited group
        /// </summary>
        /// <returns>The inherited group for user.</returns>
        /// <param name="username">Username.</param>
        Group GetInheritedGroupForUser(string username);

        #endregion
    }

    /// <summary>
    /// Expected permission cases
    /// </summary>
    public enum Permission : byte
    {
        Denied = 0,
        Permitted
    }

    /// <summary>
    /// Direct access to the active Data Connector.
    /// </summary>
    /// <remarks>Plugins use this</remarks>
    public static class Storage
    {
        private static readonly object _sync = new object();
        //        private static IDataConnector _connector;

        /// <summary>
        /// Gets a value indicating if there is a connector available.
        /// </summary>
        /// <value><c>true</c> if is available; otherwise, <c>false</c>.</value>
        public static bool IsAvailable
        {
            internal set;
            get;
        }

        /// <summary>
        /// Determines if a player is permitted for a node
        /// </summary>
        /// <returns><c>true</c> if is permitted the specified node player; otherwise, <c>false</c>.</returns>
        /// <param name="node">Node.</param>
        /// <param name="player">Player.</param>
        public static Permission IsPermitted(string node, BasePlayer player)
        {
//            if (IsAvailable)
            return player.Op ? Permission.Permitted : Permission.Denied;
//            return _connector.IsPermitted(node, player);
        }

        /// <summary>
        /// Find a group by name
        /// </summary>
        /// <returns>The group.</returns>
        /// <param name="name">Name.</param>
        public static Group FindGroup(string name)
        {
            if (IsAvailable)
                throw new InvalidOperationException("No connector attached");
           
            using (var ctx = new OTAContext()) return ctx.Groups.SingleOrDefault(x => x.Name == name);
        }

        /// <summary>
        /// Add or update a group
        /// </summary>
        /// <returns><c>true</c>, if the update group was added/updated, <c>false</c> otherwise.</returns>
        /// <param name="name">Name.</param>
        /// <param name="applyToGuests">If set to <c>true</c>, the group will be applied to guests.</param>
        /// <param name="parent">Parent.</param>
        /// <param name="r">The red chat component.</param>
        /// <param name="g">The green chat component.</param>
        /// <param name="b">The blue chat component.</param>
        /// <param name="prefix">Prefix.</param>
        /// <param name="suffix">Suffix.</param>
        public async static Task<Group> AddOrUpdateGroup(string name, bool applyToGuests = false, string parent = null, byte r = 255, byte g = 255, byte b = 255, string prefix = null, string suffix = null)
        {
            using (var ctx = new OTAContext())
            {
                var group = ctx.Groups.SingleOrDefault(x => x.Name == name);
                if (group != null)
                {
                    group.ApplyToGuests = applyToGuests;
                    group.Parent = parent;
                    group.Chat_Red = r;
                    group.Chat_Green = g;
                    group.Chat_Blue = b;
                    group.Chat_Prefix = prefix;
                    group.Chat_Suffix = suffix;
                }
                else
                {
                    ctx.Groups.Add(group = new Group()
                        {
                            ApplyToGuests = applyToGuests,
                            Parent = parent,
                            Chat_Red = r,
                            Chat_Green = g,
                            Chat_Blue = b,
                            Chat_Prefix = prefix,
                            Chat_Suffix = suffix
                        });
                }

                await ctx.SaveChangesAsync();

                return group;
            }
        }

        public static async Task<NodePermission> FindOrCreateNode(string node, Permission permission)
        {
            using (var ctx = new OTAContext())
            {
                var existing = ctx.Nodes.SingleOrDefault(x => x.Node == node && x.Permission == permission);
                if (existing != null) return existing;
                else
                {
                    ctx.Nodes.Add(existing = new NodePermission()
                        {
                            Node = node,
                            Permission = permission
                        });

                    await ctx.SaveChangesAsync();

                    return existing;
                }
            }
        }

        /// <summary>
        /// Remove a group
        /// </summary>
        /// <returns><c>true</c>, if group was removed, <c>false</c> otherwise.</returns>
        /// <param name="name">Name.</param>
        public static async Task<bool> RemoveGroup(string name)
        {
            if (IsAvailable)
                throw new InvalidOperationException("No connector attached");
           
            using (var ctx = new OTAContext())
            {
                var range = ctx.Groups.RemoveRange(ctx.Groups.Where(x => x.Name == name));
                await ctx.SaveChangesAsync();

                return range.Any();
            }
        }

        /// <summary>
        /// Adds a node to a group
        /// </summary>
        /// <returns><c>true</c>, if group node was added, <c>false</c> otherwise.</returns>
        /// <param name="groupName">Group name.</param>
        /// <param name="node">Node.</param>
        /// <param name="deny">If set to <c>true</c> deny.</param>
        public static async Task<bool> AddGroupNode(string groupName, string node, Permission permission)
        {
            if (IsAvailable)
                throw new InvalidOperationException("No connector attached");

            using (var ctx = new OTAContext())
            {
                var group = ctx.Groups.Where(x => x.Name == groupName).SingleOrDefault();
                var perm = await FindOrCreateNode(node, permission);

                ctx.GroupNodes.Add(new GroupNode()
                    {
                        GroupId = group.Id,
                        NodeId = perm.Id
                    });

                await ctx.SaveChangesAsync();

                return true;
            }
        }

        /// <summary>
        /// Removes a node from a group
        /// </summary>
        /// <returns><c>true</c>, if group node was removed, <c>false</c> otherwise.</returns>
        /// <param name="groupName">Group name.</param>
        /// <param name="node">Node.</param>
        /// <param name="deny">If set to <c>true</c> deny.</param>
        public static async Task<bool> RemoveGroupNode(string groupName, string node, Permission permission)
        {
            if (IsAvailable)
                throw new InvalidOperationException("No connector attached");

            using (var ctx = new OTAContext())
            {
                var range = ctx.GroupNodes.RemoveRange(
                                from grp in ctx.Groups
                                               join nds in ctx.GroupNodes on grp.Id equals nds.GroupId
                                               select nds
                            );

                await ctx.SaveChangesAsync();

                return range.Any();
            }
        }

        /// <summary>
        /// Fetches the group names available
        /// </summary>
        /// <returns>The list.</returns>
        public static string[] GroupList()
        {
            if (IsAvailable)
                throw new InvalidOperationException("No connector attached");

            using (var ctx = new OTAContext())
            {
                return ctx.Groups.Select(x => x.Name).ToArray();
            }
        }

        /// <summary>
        /// Fetches the nodes for a group
        /// </summary>
        /// <returns>The nodes.</returns>
        /// <param name="groupName">Group name.</param>
        public static NodePermission[] GroupNodes(string groupName)
        {
            if (IsAvailable)
                throw new InvalidOperationException("No connector attached");

            using (var ctx = new OTAContext())
            {
                return ctx.Groups
                    .Where(g => g.Name == groupName)
                    .Join(ctx.GroupNodes, grp => grp.Id, gn => gn.GroupId, (a, b) => b)
                    .Join(ctx.Nodes, gp => gp.Id, nd => nd.Id, (a, b) => b)
                    .ToArray();
            }
        }

        /// <summary>
        /// Adds a user to a group
        /// </summary>
        /// <returns><c>true</c>, if user to group was added, <c>false</c> otherwise.</returns>
        /// <param name="username">Username.</param>
        /// <param name="groupName">Group name.</param>
        public static async Task<bool> AddUserToGroup(string username, string groupName)
        {
            if (IsAvailable)
                throw new InvalidOperationException("No connector attached");

            using (var ctx = new OTAContext())
            {
                var user = ctx.Players.Single(x => x.Name == username);
                var group = ctx.Groups.Single(x => x.Name == groupName);

                //Temporary until the need for more than one group
                if (ctx.PlayerGroups.Any(x => x.GroupId > 0))
                    throw new NotSupportedException("A player can only be associated to one group, please assign a parent to the desired group");

                ctx.PlayerGroups.Add(new PlayerGroup()
                    {
                        GroupId = group.Id,
                        UserId = user.Id
                    });

                await ctx.SaveChangesAsync();

                return true;
            }
        }

        /// <summary>
        /// Removes a player from a group
        /// </summary>
        /// <returns><c>true</c>, if user from group was removed, <c>false</c> otherwise.</returns>
        /// <param name="username">Username.</param>
        /// <param name="groupName">Group name.</param>
        public static async Task<bool> RemoveUserFromGroup(string username, string groupName)
        {
            if (IsAvailable)
                throw new InvalidOperationException("No connector attached");

            using (var ctx = new OTAContext())
            {
                var user = ctx.Players.Single(x => x.Name == username);
                var group = ctx.Groups.Single(x => x.Name == groupName);

                var range = ctx.PlayerGroups.RemoveRange(ctx.PlayerGroups.Where(x =>
                                    x.GroupId == group.Id &&
                                    x.UserId == user.Id
                                ));

                await ctx.SaveChangesAsync();

                return range.Any();
            }
        }

        /// <summary>
        /// Adds a specific node to a user
        /// </summary>
        /// <returns><c>true</c>, if node to user was added, <c>false</c> otherwise.</returns>
        /// <param name="username">Username.</param>
        /// <param name="node">Node.</param>
        /// <param name="deny">If set to <c>true</c> deny.</param>
        public static async Task<bool> AddNodeToUser(string username, string node, Permission permission)
        {
            if (IsAvailable)
                throw new InvalidOperationException("No connector attached");

            using (var ctx = new OTAContext())
            {
                var user = ctx.Players.Single(x => x.Name == username);
                var perm = await FindOrCreateNode(node, permission);

                var range = ctx.PlayerNodes.RemoveRange(ctx.PlayerNodes.Where(x =>
                                    x.NodeId == perm.Id &&
                                    x.UserId == user.Id
                                ));

                await ctx.SaveChangesAsync();

                return range.Any();
            }
        }

        /// <summary>
        /// Removes a specific node from a user
        /// </summary>
        /// <returns><c>true</c>, if node from user was removed, <c>false</c> otherwise.</returns>
        /// <param name="username">Username.</param>
        /// <param name="node">Node.</param>
        /// <param name="deny">If set to <c>true</c> deny.</param>
        public static async Task<bool> RemoveNodeFromUser(string username, string node, Permission permission)
        {
            if (IsAvailable)
                throw new InvalidOperationException("No connector attached");

            using (var ctx = new OTAContext())
            {
                var range = ctx.PlayerNodes.RemoveRange(
                                ctx.Players
                                    .Where(p => p.Name == username)
                                    .Join(ctx.PlayerNodes, x => x.Id, y => y.UserId, (a, b) => b)
                            );

                await ctx.SaveChangesAsync();

                return range.Any();
            }
        }

        /// <summary>
        /// Fetches the associated groups names for a user
        /// </summary>
        /// <returns>The group list.</returns>
        /// <param name="username">Username.</param>
        public static string[] UserGroupList(string username)
        {
            if (IsAvailable)
                throw new InvalidOperationException("No connector attached");

            using (var ctx = new OTAContext())
            {
                return ctx.Players
                .Where(p => p.Name == username)
                    .Join(ctx.PlayerGroups, pg => pg.Id, y => y.UserId, (a, b) => b)
                    .Join(ctx.Groups, pg => pg.Id, g => g.Id, (a, b) => b)
                    .Select(x => x.Name)
                    .ToArray();
            }
        }

        /// <summary>
        /// Fetches the list of nodes for a user
        /// </summary>
        /// <returns>The nodes.</returns>
        /// <param name="username">Username.</param>
        public static NodePermission[] UserNodes(string username)
        {
            if (IsAvailable)
                throw new InvalidOperationException("No connector attached");

            using (var ctx = new OTAContext())
            {
                return ctx.Players
                    .Where(p => p.Name == username)
                    .Join(ctx.PlayerNodes, pn => pn.Id, y => y.UserId, (a, b) => b)
                    .Join(ctx.Nodes, pn => pn.Id, nd => nd.Id, (a, b) => b)
                    .ToArray();
            }
        }

        /// <summary>
        /// Fetches the lower most group for a player
        /// </summary>
        /// <remarks>There should always be one at this stage in OTA. The flexibility is just here.</remarks>
        /// <returns>The inherited group for user.</returns>
        /// <param name="username">Username.</param>
        public static Group GetInheritedGroupForUser(string username)
        {
            if (IsAvailable)
                throw new InvalidOperationException("No connector attached");
            
            using (var ctx = new OTAContext())
            {
                return ctx.Players
                    .Where(x => x.Name == username)
                    .Join(ctx.PlayerGroups, pg => pg.Id, us => us.UserId, (a, b) => b)
                    .Join(ctx.Groups, pg => pg.GroupId, gr => gr.Id, (a, b) => b)
                    .FirstOrDefault();
            }
        }
    }
}

