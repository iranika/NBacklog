﻿using NBacklog.Query;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace NBacklog.DataTypes
{
    public class ProjectDiskUsage : BacklogItem
    {
        public int Issue { get; }
        public int Wiki { get; }
        public int File { get; }
        public int Subversion { get; }
        public int Git { get; }

        internal ProjectDiskUsage(_ProjectDiskUsage data)
            : base(-1)
        {
            Issue = data.issue;
            Wiki = data.wiki;
            File = data.file;
            Subversion = data.subversion;
            Git = data.git;
        }
    }

    public class Project : CachableBacklogItem
    {
        public BacklogClient Client { get; }

        public string Key { get; set; }
        public string Name { get; set; }
        public bool IsChartEnabled { get; set; }
        public bool IsSubtaskingEnabled { get; set; }
        public bool CanProjectLeaderEditProjectLeader { get; set; }
        public string TextFormattingRule { get; set; }
        public bool IsArchived { get; set; }

        internal Project(_Project data, BacklogClient client)
            : base(data.id)
        {
            Key = data.projectKey;
            Name = data.name;
            IsChartEnabled = data.chartEnabled;
            IsSubtaskingEnabled = data.subtaskingEnabled;
            CanProjectLeaderEditProjectLeader = data.projectLeaderCanEditProjectLeader;
            TextFormattingRule = data.textFormattingRule;
            IsArchived = data.archived;

            Client = client;
        }

        #region users
        public async Task<BacklogResponse<User[]>> GetUsersAsync()
        {
            var response = await Client.GetAsync($"/api/v2/projects/{Id}/users").ConfigureAwait(false);
            return await Client.CreateResponseAsync<User[], List <_User>>(
                response,
                HttpStatusCode.OK,
                data => data.Select(x => Client.ItemsCache.Update(x.id, () => new User(x, Client))).ToArray()
                ).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<User>> AddUserAsync(User user)
        {
            var parameters = new
            {
                userId = user.Id,
            };

            var response = await Client.PostAsync($"/api/v2/projects/{Id}/users", parameters).ConfigureAwait(false);
            return await Client.CreateResponseAsync<User, _User>(
                response,
                HttpStatusCode.Created,
                data => Client.ItemsCache.Update(data.id, () => new User(data, Client))
                ).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<User>> DeleteUserAsync(User user)
        {
            var parameters = new
            {
                userId = user.Id,
            };

            var response = await Client.DeleteAsync($"/api/v2/projects/{Id}/users", parameters).ConfigureAwait(false);
            return await Client.CreateResponseAsync<User, _User>(
                response,
                HttpStatusCode.OK,
                data => Client.ItemsCache.Delete<User>(data.id)
                ).ConfigureAwait(false);
        }
        #endregion

        #region tickets
        public async Task<BacklogResponse<Ticket[]>> GetTicketsAsync(TicketQuery query = null)
        {
            query = query ?? new TicketQuery();
            query.Project(this);

            var response = await Client.GetAsync("/api/v2/issues", query.Build()).ConfigureAwait(false);
            return await Client.CreateResponseAsync<Ticket[], List<_Ticket>>(
                response,
                HttpStatusCode.OK,
                data => data.Select(x => Client.ItemsCache.Update(x.id, () => new Ticket(x, this, Client))).ToArray()
                ).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<int>> GetTicketCountAsync(TicketQuery query = null)
        {
            query = query ?? new TicketQuery();
            query.Project(this);

            var response = await Client.GetAsync("/api/v2/issues/count", query.Build()).ConfigureAwait(false);
            return await Client.CreateResponseAsync<int, _Count>(
                response,
                HttpStatusCode.OK,
                data => data.count).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<Ticket>> AddTicketAsync(Ticket ticket)
        {
            var parameters = ticket.ToApiParameters();
            parameters.Replace("projectId", Id);

            var response = await Client.PostAsync("/api/v2/issues", parameters.Build()).ConfigureAwait(false);
            return await Client.CreateResponseAsync<Ticket, _Ticket>(
                response,
                HttpStatusCode.Created,
                data => Client.ItemsCache.Update(data.id, () => new Ticket(data, this, Client))
                ).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<Ticket>> UpdateTicketAsync(Ticket ticket)
        {
            var parameters = ticket.ToApiParameters();
            parameters.Replace("projectId", Id);

            var response = await Client.GetAsync($"/api/v2/issues/{ticket.Id}", parameters.Build()).ConfigureAwait(false);
            return await Client.CreateResponseAsync<Ticket, _Ticket>(
                response,
                HttpStatusCode.OK,
                data => Client.ItemsCache.Update(data.id, () => new Ticket(data, this, Client))
                ).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<Ticket>> DeleteTicketAsync(Ticket ticket)
        {
            var response = await Client.DeleteAsync($"/api/v2/issues/{ticket.Id}").ConfigureAwait(false);
            return await Client.CreateResponseAsync<Ticket, _Ticket>(
                response,
                HttpStatusCode.OK,
                data => Client.ItemsCache.Delete<Ticket>(data.id)
                ).ConfigureAwait(false);
        }
        #endregion

        #region type
        public async Task<BacklogResponse<TicketType[]>> GetTicketTypesAsync()
        {
            var response = await Client.GetAsync($"/api/v2/projects/{Id}/issueTypes").ConfigureAwait(false);
            return await Client.CreateResponseAsync<TicketType[], List<_TicketType>>(
                response,
                HttpStatusCode.OK,
                data => data.Select(x => Client.ItemsCache.Update(x.id, () => new TicketType(x, this))).ToArray()
                ).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<TicketType>> AddTicketTypeAsync(TicketType type)
        {
            var parameters = type.ToApiParameters();
            var response = await Client.PostAsync($"/api/v2/projects/{Id}/issueTypes", parameters.Build()).ConfigureAwait(false);
            return await Client.CreateResponseAsync<TicketType, _TicketType>(
                response,
                HttpStatusCode.OK,
                data => Client.ItemsCache.Update(data.id, () => new TicketType(data, this))
                ).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<TicketType>> UpdateTicketTypeAsync(TicketType type)
        {
            var parameters = type.ToApiParameters();
            var response = await Client.PatchAsync($"/api/v2/projects/{Id}/issueTypes/{type.Id}", parameters.Build()).ConfigureAwait(false);
            return await Client.CreateResponseAsync<TicketType, _TicketType>(
                response,
                HttpStatusCode.OK,
                data => Client.ItemsCache.Update(data.id, () => new TicketType(data, this))
                ).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<TicketType>> DeleteTicketAsync(TicketType type)
        {
            var response = await Client.DeleteAsync($"/api/v2/projects/{Id}/issueTypes/{type.Id}").ConfigureAwait(false);
            return await Client.CreateResponseAsync<TicketType, _TicketType>(
                response,
                HttpStatusCode.OK,
                data => Client.ItemsCache.Delete<TicketType>(data.id)
                ).ConfigureAwait(false);
        }
        #endregion

        #region category
        public async Task<BacklogResponse<Category[]>> GetCategoriesAsync()
        {
            var response = await Client.GetAsync($"/api/v2/projects/{Id}/categories").ConfigureAwait(false);
            return await Client.CreateResponseAsync<Category[], List<_Category>>(
                response,
                HttpStatusCode.OK,
                data => data.Select(x => Client.ItemsCache.Update(x.id, () => new Category(x))).ToArray()
                ).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<Category>> AddCategoryAsync(Category category)
        {
            var parameters = category.ToApiParameters();
            var response = await Client.PostAsync($"/api/v2/projects/{Id}/categories", parameters.Build()).ConfigureAwait(false);
            return await Client.CreateResponseAsync<Category, _Category>(
                response,
                HttpStatusCode.OK,
                data => Client.ItemsCache.Update(data.id, () => new Category(data))
                ).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<Category>> UpdateCategoryAsync(Category category)
        {
            var parameters = category.ToApiParameters();
            var response = await Client.PatchAsync($"/api/v2/projects/{Id}/categories", parameters.Build()).ConfigureAwait(false);
            return await Client.CreateResponseAsync<Category, _Category>(
                response,
                HttpStatusCode.OK,
                data => Client.ItemsCache.Update(data.id, () => new Category(data))
                ).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<Category>> DeleteCategoryAsync(Category category)
        {
            var parameters = category.ToApiParameters();
            var response = await Client.DeleteAsync($"/api/v2/projects/{Id}/categories").ConfigureAwait(false);
            return await Client.CreateResponseAsync<Category, _Category>(
                response,
                HttpStatusCode.OK,
                data => Client.ItemsCache.Update(data.id, () => new Category(data))
                ).ConfigureAwait(false);
        }
        #endregion

        #region milestone
        public async Task<BacklogResponse<Milestone[]>> GetMilestonesAsync()
        {
            var response = await Client.GetAsync($"/api/v2/projects/{Id}/versions").ConfigureAwait(false);
            return await Client.CreateResponseAsync<Milestone[], List<_Milestone>>(
                response,
                HttpStatusCode.OK,
                data => data.Select(x => Client.ItemsCache.Update(x.id, () => new Milestone(x, this))).ToArray()
                ).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<Milestone>> AddMilestoneAsync(Milestone milestone)
        {
            var parameters = milestone.ToApiParameters();
            var response = await Client.PostAsync($"/api/v2/projects/{Id}/versions", parameters.Build()).ConfigureAwait(false);
            return await Client.CreateResponseAsync<Milestone, _Milestone>(
                response,
                HttpStatusCode.OK,
                data => Client.ItemsCache.Update(data.id, () => new Milestone(data, this))
                ).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<Milestone>> UpdateMilestoneAsync(Milestone milestone)
        {
            var parameters = milestone.ToApiParameters();
            var response = await Client.PatchAsync($"/api/v2/projects/{Id}/versions/{milestone.Id}", parameters.Build()).ConfigureAwait(false);
            return await Client.CreateResponseAsync<Milestone, _Milestone>(
                response,
                HttpStatusCode.OK,
                data => Client.ItemsCache.Update(data.id, () => new Milestone(data, this))
                ).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<Milestone>> DeleteMilestoneAsync(Milestone milestone)
        {
            var response = await Client.DeleteAsync($"/api/v2/projects/{Id}/versions/{milestone.Id}").ConfigureAwait(false);
            return await Client.CreateResponseAsync<Milestone, _Milestone>(
                response,
                HttpStatusCode.OK,
                data => Client.ItemsCache.Delete<Milestone>(data.id)).ConfigureAwait(false);
        }
        #endregion

        #region custom filed
        public async Task<BacklogResponse<CustomField[]>> GetCustomFieldsAsync()
        {
            var response = await Client.GetAsync($"/api/v2/projects/{Id}/customFields").ConfigureAwait(false);
            return await Client.CreateResponseAsync<CustomField[], List<_CustomField>>(
                response,
                HttpStatusCode.OK,
                data => data.Select(x => Client.ItemsCache.Update(x.id, () => CustomField.Create(x, this))).ToArray()
                ).ConfigureAwait(false);
        }
        #endregion

        #region webhook
        public async Task<BacklogResponse<Webhook[]>> GetWebhooksAsync()
        {
            var response = await Client.GetAsync($"/api/v2/projects/{Id}/webhooks").ConfigureAwait(false);
            return await Client.CreateResponseAsync<Webhook[], List<_Webhook>>(
                response,
                HttpStatusCode.OK,
                data => data.Select(x => Client.ItemsCache.Update(x.id, () => new Webhook(x, this))).ToArray()
                ).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<Webhook>> AddWebhookAsync(Webhook hook)
        {
            var parameters = hook.ToApiParameters();

            var response = await Client.PostAsync($"/api/v2/projects/{Id}/webhooks", parameters.Build()).ConfigureAwait(false);
            return await Client.CreateResponseAsync<Webhook, _Webhook>(
                response,
                HttpStatusCode.OK,
                data => Client.ItemsCache.Update(data.id, () => new Webhook(data, this))
                ).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<Webhook>> UpdateWebhookAsync(Webhook hook)
        {
            var parameters = hook.ToApiParameters();

            var response = await Client.PatchAsync($"/api/v2/projects/{Id}/webhooks/{hook.Id}", parameters.Build()).ConfigureAwait(false);
            return await Client.CreateResponseAsync<Webhook, _Webhook>(
                response,
                HttpStatusCode.OK,
                data => Client.ItemsCache.Update(data.id, () => new Webhook(data, this))
                ).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<Webhook>> DeleteWebhookAsync(Webhook hook)
        {
            var parameters = hook.ToApiParameters();

            var response = await Client.DeleteAsync($"/api/v2/projects/{Id}/webhooks/{hook.Id}", parameters.Build()).ConfigureAwait(false);
            return await Client.CreateResponseAsync<Webhook, _Webhook>(
                response,
                HttpStatusCode.OK,
                data => Client.ItemsCache.Delete<Webhook>(data.id)).ConfigureAwait(false);
        }
        #endregion

        public async Task<BacklogResponse<SharedFile[]>> GetSharedFilesAsync(string directory = "", SharedFileQuery query = null)
        {
            query = query ?? new SharedFileQuery();

            var response = await Client.GetAsync($"/api/v2/projects/{Id}/files/metadata/{directory}", query.Build()).ConfigureAwait(false);
            var result = await Client.CreateResponseAsync<SharedFile[], List<_SharedFile>>(
                response,
                HttpStatusCode.OK,
                data => data.Select(x => Client.ItemsCache.Update(x.id, () => new SharedFile(x, this))).ToArray()
                ).ConfigureAwait(false);

            if (result.Errors != null)
            {
                return result;
            }

            var content = result.Content.Where(x => query.TypeNames.Contains(x.TypeName)).ToArray();
            if (content.Length == result.Content.Length)
            {
                return result;
            }

            return new BacklogResponse<SharedFile[]>(result.StatusCode, content);
        }

        public async Task<BacklogResponse<MemoryStream>> GetIconAsync()
        {
            var response = await Client.GetAsync($"/api/v2/projects/{Id}/icon").ConfigureAwait(false); ;
            return await Client.CreateResponseAsync(
                response,
                HttpStatusCode.OK,
                data => new MemoryStream(data)).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<Activity[]>> GetActivitiesAsync(ActivityQuery query = null)
        {
            query = query ?? new ActivityQuery();
            var response = await Client.GetAsync($"/api/v2/projects/{Id}/activities", query.Build()).ConfigureAwait(false);
            return await Client.CreateResponseAsync<Activity[], List<_Activity>>(
                response,
                HttpStatusCode.OK,
                data => data.Select(x => new Activity(x, Client)).ToArray()).ConfigureAwait(false);
        }

        public async Task<BacklogResponse<ProjectDiskUsage>> GetDiskUsageAsync()
        {
            var response = await Client.GetAsync($"/api/v2/projects/{Id}/diskUsage").ConfigureAwait(false);
            return await Client.CreateResponseAsync<ProjectDiskUsage, _ProjectDiskUsage>(
                response,
                HttpStatusCode.OK,
                data => new ProjectDiskUsage(data)).ConfigureAwait(false);
        }
    }
}
