﻿using NBacklog.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace NBacklog.DataTypes
{
    public class TicketSummary : BacklogItem
    {
        public int KeyId { get; }
        public string Summary { get; }
        public string Description { get; }

        internal TicketSummary(_TicketSummary data)
            : base(data.id)
        {
            KeyId = data.keyId;
            Summary = data.summary;
            Description = data.description;
        }
    }

    public class Status : CachableBacklogItem
    {
        public string Name { get; set; }

        internal Status(_Status data)
            : base(data.id)
        {
            Name = data.name;
        }
    }

    public class Resolution : CachableBacklogItem
    {
        public string Name { get; set; }

        public Resolution(int id)
            : base(id)
        { }

        internal Resolution(_Resolution data)
            : base(data.id)
        {
            Name = data.name;
        }
    }

    public class Priority : CachableBacklogItem
    {
        public string Name { get; set; }

        internal Priority(_Priority data)
            : base(data.id)
        {
            Name = data.name;
        }
    }

    public class Ticket : CachableBacklogItem
    {
        public Project Project { get; }

        public string Key { get; }
        public int KeyId { get; }
        public TicketType Type { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public Resolution Resolution { get; set; }
        public Priority Priority { get; set; }
        public Status Status { get; set; }
        public User Assignee { get; set; }
        public Category[] Categories { get; set; } = Array.Empty<Category>();
        public Milestone[] Versions { get; set; } = Array.Empty<Milestone>();
        public Milestone[] Milestones { get; set; } = Array.Empty<Milestone>();
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public double? EstimatedHours { get; set; }
        public double? ActualHours { get; set; }
        public int? ParentTicketId { get; set; }
        public User Creator { get; }
        public DateTime Created { get; }
        public User LastUpdater { get; }
        public DateTime? LastUpdated { get; }
        public CustomFieldValue[] CustomFields { get; set; }
        public Attachment[] Attachments { get; set; } = Array.Empty<Attachment>();
        public SharedFile[] SharedFiles { get; private set; } = Array.Empty<SharedFile>();
        public Star[] Stars { get; } = Array.Empty<Star>();

        public Ticket(int id)
            : base(id)
        { }

        public Ticket(string key)
            : base(-1)
        {
            Key = key;
        }

        public Ticket(string summary, TicketType type, Priority priority)
            : base(-1)
        {
            Summary = summary;
            Type = type;
            Priority = priority;
        }

        internal Ticket(_Ticket data, Project project, BacklogClient client)
            : base(data.id)
        {
            Project = project;
            Key = data.issueKey;
            KeyId = data.keyId;
            Type = client.ItemsCache.Update(data.issueType?.id, () => new TicketType(data.issueType, project));
            Summary = data.summary;
            Description = data.description;
            Resolution = client.ItemsCache.Update(data.resolution?.id, () => new Resolution(data.resolution));
            Priority = client.ItemsCache.Update(data.priority?.id, () => new Priority(data.priority));
            Status = client.ItemsCache.Update(data.status?.id, () => new Status(data.status));
            Assignee = client.ItemsCache.Update(data.assignee?.id, () => new User(data.assignee, client));
            Categories = data.category.Select(x => client.ItemsCache.Update(x.id, () => new Category(x))).ToArray();
            Versions = data.versions.Select(x => client.ItemsCache.Update(x.id, () => new Milestone(x, project))).ToArray();
            Milestones = data.milestone.Select(x => client.ItemsCache.Update(x.id, () => new Milestone(x, project))).ToArray();
            StartDate = data.startDate?.Date;
            DueDate = data.dueDate?.Date;
            EstimatedHours = data.estimatedHours;
            ActualHours = data.actualHours;
            ParentTicketId = data.parentIssueId;
            Creator = client.ItemsCache.Update(data.createdUser?.id, () => new User(data.createdUser, client));
            Created = data.created;
            LastUpdater = client.ItemsCache.Update(data.updatedUser?.id, () => new User(data.updatedUser, client));
            LastUpdated = data.updated;
            CustomFields = data.customFields.Select(x => new CustomFieldValue(x)).ToArray();
            Attachments = data.attachments.Select(x => new Attachment(x, this)).ToArray();
            SharedFiles = data.sharedFiles.Select(x => new SharedFile(x, project)).ToArray();
            Stars = data.stars.Select(x => new Star(x, client)).ToArray();

            _client = client;
        }

        #region comment
        public async Task<BacklogResponse<int>> GetCommentCountAsync(CommentQuery query = null)
        {
            if (Id < 0)
            {
                throw new InvalidOperationException("ticket retrieved not from the server");
            }

            query = query ?? new CommentQuery();

            var response = await _client.GetAsync($"/api/v2/issues/{Id}/comments/count", query.Build()).ConfigureAwait(false);
            return await _client.CreateResponseAsync<int, _Count>(
                response,
                HttpStatusCode.OK,
                data => data.count).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<Comment[]>> GetCommentsAsync(CommentQuery query = null)
        {
            if (Id < 0)
            {
                throw new InvalidOperationException("ticket retrieved not from the server");
            }

            query = query ?? new CommentQuery();

            var response = await _client.GetAsync($"/api/v2/issues/{Id}/comments", query.Build()).ConfigureAwait(false);
            return await _client.CreateResponseAsync<Comment[], List<_Comment>>(
                response,
                HttpStatusCode.OK,
                data => data.Select(x => new Comment(x, this)).ToArray()).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<Comment>> AddCommentAsync(Comment comment, IEnumerable<User> notifiedUsers = null, IEnumerable<Attachment> attachments = null)
        {
            if (Id < 0)
            {
                throw new InvalidOperationException("ticket retrieved not from the server");
            }

            var parameters = new List<(string, object)>
            {
                ("content", comment.Content),
            };

            if (notifiedUsers != null)
            {
                foreach (var user in notifiedUsers)
                {
                    parameters.Add(("notifiedUserId[]", user.Id));
                }
            }
            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    parameters.Add(("attachmentId[]", attachment.Id));
                }
            }

            var response = await _client.PostAsync($"/api/v2/issues/{Id}/comments", parameters).ConfigureAwait(false);
            return await _client.CreateResponseAsync<Comment, _Comment>(
                response,
                HttpStatusCode.Created,
                data => new Comment(data, this)).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<Comment>> UpdateCommentAsync(Comment comment)
        {
            if (Id < 0)
            {
                throw new InvalidOperationException("ticket retrieved not from the server");
            }

            var parameters = new
            {
                content = comment.Content,
            };

            var response = await _client.PatchAsync($"/api/v2/issues/{Id}/comments/{comment.Id}", parameters).ConfigureAwait(false);
            return await _client.CreateResponseAsync<Comment, _Comment>(
                response,
                HttpStatusCode.OK,
                data => new Comment(data, this)).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<Comment>> DeleteCommentAsync(Comment comment)
        {
            if (Id < 0)
            {
                throw new InvalidOperationException("ticket retrieved not from the server");
            }

            var parameters = new
            {
                content = comment.Content,
            };

            var response = await _client.DeleteAsync($"/api/v2/issues/{Id}/comments/{comment.Id}", parameters).ConfigureAwait(false);
            return await _client.CreateResponseAsync<Comment, _Comment>(
                response,
                HttpStatusCode.OK,
                data => new Comment(data, this)).ConfigureAwait(false);
        }
        #endregion

        #region shared files
        public async Task<BacklogResponse<SharedFile[]>> LinkSharedFilesAsync(params SharedFile[] sharedFiles)
        {
            if (Id < 0)
            {
                throw new InvalidOperationException("ticket retrieved not from the server");
            }

            var parameters = new QueryParameters();
            parameters.AddRange("fileId[]", sharedFiles.Select(x => x.Id));

            var response = await _client.PostAsync($"/api/v2/issues/{Id}/sharedFiles", parameters.Build()).ConfigureAwait(false);
            var result = await _client.CreateResponseAsync<SharedFile[], List<_SharedFile>>(
                response,
                HttpStatusCode.OK,
                data => data.Select(x => _client.ItemsCache.Update(x.id, () => new SharedFile(x, Project))).ToArray()
                ).ConfigureAwait(false);

            if (result.Content.Length > 0)
            {
                SharedFiles = SharedFiles.Concat(result.Content).ToArray();
            }

            return result;
        }

        public async Task<BacklogResponse<SharedFile>> UnlinkSharedFilesAsync(SharedFile sharedFile)
        {
            if (Id < 0)
            {
                throw new InvalidOperationException("ticket retrieved not from the server");
            }

            var response = await _client.DeleteAsync($"/api/v2/issues/{Id}/sharedFiles/{sharedFile.Id}").ConfigureAwait(false);
            var result = await _client.CreateResponseAsync<SharedFile, _SharedFile>(
                response,
                HttpStatusCode.OK,
                data => _client.ItemsCache.Delete<SharedFile>(data.id)).ConfigureAwait(false);

            if (result.Content != null)
            {
                SharedFiles = SharedFiles.Except(new[] { result.Content }).ToArray();
            }

            return result;
        }
        #endregion

        internal QueryParameters ToApiParameters(bool toCreate)
        {
            var parameters = new QueryParameters();

            parameters.Add("summary", Summary, toCreate);
            parameters.Add("issueTypeId", Type.Id, toCreate);
            parameters.Add("priorityId", Priority.Id, toCreate);

            parameters.Add("description", Description ?? "");
            parameters.Add("dueDate", DueDate?.ToString("yyyy-MM-dd") ?? "");
            parameters.Add("startDate", StartDate?.ToString("yyyy-MM-dd") ?? "");
            parameters.AddRange("categoryId[]", Categories.Select(x => x.Id));
            parameters.AddRange("versionId[]", Versions.Select(x => x.Id));
            parameters.AddRange("milestoneId[]", Milestones.Select(x => x.Id));
            parameters.AddRange("attachmentId[]", Attachments.Select(x => x.Id));
            parameters.Add("parentIssueId", ParentTicketId?.ToString() ?? "");
            parameters.Add("estimatedHours", EstimatedHours?.ToString() ?? "");
            parameters.Add("actualHours", ActualHours?.ToString() ?? "");
            parameters.Add("assigneeId", Assignee?.Id.ToString() ?? "");

            if (!toCreate)
            {
                parameters.Add("statusId", Status?.Id.ToString() ?? "");
                parameters.Add("resolutionId", Resolution?.Id.ToString() ?? "");
            }

            if (CustomFields != null)
            {
                foreach (var field in CustomFields)
                {
                    parameters.Add($"customField_{field.Id}", field.ToJsonValue());
                    if (field.OtherValue != null)
                    {
                        parameters.Add($"customField_{field.Id}_otherValue", field.OtherValue);
                    }
                }
            }

            return parameters;
        }

        private BacklogClient _client;
    }
}
