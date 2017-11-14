using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq;
using System.Threading.Tasks;
using Tone.Core;
using Tone.Core.Data;
using Tone.Core.Provider;
using Tone.Core.Subsystems.TelematicsKernel.Repositories;
using Tone.Data.Mongo.Base;
using Tone.Data.Mongo.Base.Extensions;
using Tone.Data.Mongo.Model;
using Account = Tone.Data.Mongo.Model.Account;

namespace Tone.TelematicsKernel.Data.Repository.Mongo
{
    public class DeviceGroupRepository : MongoRepositoryBase<Account>, IDeviceGroupRepository
    {
        public DeviceGroupRepository(IConnectionStringProvider connectionStringProvider)
            : base(Config.AccountCollection, connectionStringProvider.ConnectionString)
        {
        }

        public ITreeNode<DeviceGroup> New(ITreeNode<DeviceGroup> parent = null)
        {
            var parentNode = (TreeNode<DeviceGroup>)parent;
            var result = new TreeNode<DeviceGroup>();
            result.SetParent(parentNode);
            return result;
        }

        public object NewId()
        {
            return ObjectId.GenerateNewId().ToString();
        }

        public object EmptyId()
        {
            return ObjectId.Empty.ToString();
        }

        public object ToId(object id)
        {
            return id?.ToString();
        }

        public async Task Init()
        {
        }

        public async Task<ITreeNode<DeviceGroup>[]> GetAll(object treeId)
        {
            var accountId = treeId.ToObjectId();
            var filter = Builders<Account>.Filter.Eq(d => d.Id, accountId);
            var account = await Collection.Find(filter).FirstOrDefaultAsync();
            return account?.DeviceGrouping?.ToArray();
        }

        public async Task<ITreeNode<DeviceGroup>> GetNodeById(object treeId, object nodeId)
        {
            var accountId = treeId.ToObjectId();
            var filter = Builders<Account>.Filter.Eq(d => d.Id, accountId);
            var account = await Collection.Find(filter).FirstOrDefaultAsync();
            return account?.DeviceGrouping?.FirstOrDefault(n => n.Id.Equals(nodeId));
        }

        public async Task<ITreeNode<DeviceGroup>> Repsert(object treeId, ITreeNode<DeviceGroup> node)
        {
            UpdateDefinition<Account> update;
            FilterDefinition<Account> filter;

            var accountId = treeId.ToObjectId();
            var accountFilter = Builders<Account>.Filter.Eq(a => a.Id, accountId);
            if (node.Id?.Equals(EmptyId()) ?? true)
            {
                node.Id = NewId();
                filter = accountFilter;
                update = Builders<Account>.Update.Push(a => a.DeviceGrouping, node);
            }
            else
            {
                filter = Builders<Account>.Filter.And(accountFilter,
                    Builders<Account>.Filter.Eq("DeviceGrouping.Id", node.Id));
                update = Builders<Account>.Update.Set("DeviceGrouping.$", node);
            }

            var options = new FindOneAndUpdateOptions<Account, Account> { ReturnDocument = ReturnDocument.After };
            var result = await Collection.FindOneAndUpdateAsync(filter, update, options);
            return result?.DeviceGrouping?.FirstOrDefault(n => n.Id.Equals(node.Id));
        }

        public async Task<ITreeNode<DeviceGroup>[]> Repsert(object treeId, ITreeNode<DeviceGroup>[] nodes)
        {
            var accountId = treeId.ToObjectId();
            var items = nodes.Cast<TreeNode<DeviceGroup>>().ToList();
            foreach (var item in items.Where(item => item.Id == null || item.Id.Equals(EmptyId())))
                item.Id = (string)NewId();
            var filter = Builders<Account>.Filter.And(
                Builders<Account>.Filter.Eq(a => a.Id, accountId));
            var update = Builders<Account>.Update.Set(a => a.DeviceGrouping, items);
            var account = await
                Collection.FindOneAndUpdateAsync(filter, update,
                    new FindOneAndUpdateOptions<Account, Account>
                    {
                        IsUpsert = false,
                        ReturnDocument = ReturnDocument.After
                    });
            return account.DeviceGrouping.Cast<ITreeNode<DeviceGroup>>().ToArray();
        }

        public async Task DeleteOne(object treeId, ITreeNode<DeviceGroup> node)
        {
            var accountId = treeId.ToObjectId();
            var filter = Builders<Account>.Filter.And(
                Builders<Account>.Filter.Eq(a => a.Id, accountId));
            var update = Builders<Account>.Update.PullFilter(a => a.DeviceGrouping, f => f.Id.Equals(node.Id));
            await Collection.UpdateOneAsync(filter, update);
        }

        public async Task DeleteMany(object treeId, ITreeNode<DeviceGroup>[] nodes)
        {
            var ids = nodes.Select(n => n.Id.ToString()).ToArray();
            var accountId = treeId.ToObjectId();
            var filter = Builders<Account>.Filter.And(
                Builders<Account>.Filter.Eq(a => a.Id, accountId));
            var update = Builders<Account>.Update.PullFilter(a => a.DeviceGrouping, f => ids.Contains(f.Id));
            await Collection.UpdateOneAsync(filter, update);
        }

        public void SetRequestContext(IRequestContext requestContext)
        {
        }
    }
}