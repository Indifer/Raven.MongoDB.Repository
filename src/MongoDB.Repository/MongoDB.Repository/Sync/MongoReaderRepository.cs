﻿using MongoDB.Bson;
using MongoDB.Driver;
using Repository.IEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Repository
{
    /// <summary>
    /// 读取仓储
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public partial class MongoReaderRepository<TEntity, TKey> : MongoBaseRepository<TEntity, TKey>, IReaderRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>, new()
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connString">数据库连接节点</param>
        /// <param name="dbName">数据库名称</param>
        /// <param name="writeConcern"></param>
        /// <param name="readPreference"></param>
        /// <param name="sequence">Mongo自增长ID数据序列对象</param>
        public MongoReaderRepository(string connString, string dbName, WriteConcern writeConcern = null, ReadPreference readPreference = null, MongoSequence sequence = null)
            :base(connString, dbName, writeConcern, readPreference, sequence)
        {
        }

        /// <summary>
        /// 创建自增长ID
        /// </summary>
        /// <returns></returns>
        public long CreateIncID(long inc = 1, int iteration = 0)
        {
            long id = 1;
            var collection = Database.GetCollection<BsonDocument>(base._sequence.SequenceName);
            var typeName = typeof(TEntity).Name;

            var query = Builders<BsonDocument>.Filter.Eq(base._sequence.CollectionName, typeName);
            var update = Builders<BsonDocument>.Update.Inc(base._sequence.IncrementID, inc);
            var options = new FindOneAndUpdateOptions<BsonDocument, BsonDocument>();
            options.IsUpsert = true;
            options.ReturnDocument = ReturnDocument.After;

            var result = collection.FindOneAndUpdate(query, update, options);
            if (result != null)
            {
                id = result[base._sequence.IncrementID].AsInt64;
                return id;
            }
            else if (iteration <= 1)
            {
                return CreateIncID(inc, ++iteration);
            }
            else
            {
                throw new Exception("Failed to get on the IncID");
            }
        }

        /// <summary>
        /// 创建自增ID
        /// </summary>
        /// <param name="entity"></param>
        public void CreateIncID(TEntity entity)
        {
            long _id = 0;
            _id = this.CreateIncID();
            AssignmentEntityID(entity, _id);
        }

        /// <summary>
        /// 根据id获取实体
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includeFieldExp">查询字段表达式</param>
        /// <param name="sortExp">排序表达式</param>
        /// <param name="sortType">排序方式</param>
        /// <param name="hint">hint索引</param>
        /// <param name="readConcern">访问设置</param>
        /// <returns></returns>
        public TEntity Get(TKey id, Expression<Func<TEntity, object>> includeFieldExp = null
            , Expression<Func<TEntity, object>> sortExp = null, SortType sortType = SortType.Ascending, BsonValue hint = null
            , ReadConcern readConcern = null)
        {
            var filter = Builders<TEntity>.Filter.Eq(x => x.ID, id);
            //var cursor = await _mongoSession.FindAsync(filter: filter, fieldExp: fieldExp, limit: 1);

            ProjectionDefinition<TEntity, TEntity> projection = null;
            if (includeFieldExp != null)
            {
                projection = base.IncludeFields(includeFieldExp);
            }

            var option = base.CreateFindOptions(projection, sortExp, sortType, limit: 1, hint: hint);
            var result = base.GetCollection(readConcern).FindSync(filter, option);

            return result.FirstOrDefault();
        }

        /// <summary>
        /// 根据条件获取实体
        /// </summary>
        /// <param name="filterExp">查询条件表达式</param>
        /// <param name="includeFieldExp">查询字段表达式</param>
        /// <param name="sortExp">排序表达式</param>
        /// <param name="sortType">排序方式</param>
        /// <param name="hint">hint索引</param>
        /// <param name="readConcern">访问设置</param>
        /// <returns></returns>
        public TEntity Get(Expression<Func<TEntity, bool>> filterExp, Expression<Func<TEntity, object>> includeFieldExp = null
            , Expression<Func<TEntity, object>> sortExp = null, SortType sortType = SortType.Ascending, BsonValue hint = null
            , ReadConcern readConcern = null)
        {
            FilterDefinition<TEntity> filter = null;
            ProjectionDefinition<TEntity, TEntity> projection = null;

            if (filterExp != null)
            {
                filter = Builders<TEntity>.Filter.Where(filterExp);
            }
            else
            {
                filter = Builders<TEntity>.Filter.Empty;
            }

            if (includeFieldExp != null)
            {
                projection = base.IncludeFields(includeFieldExp);
            }
            var option = base.CreateFindOptions(projection, sortExp, sortType, limit: 1, hint: hint);
            var result = base.GetCollection(readConcern).FindSync(filter, option);

            return result.FirstOrDefault();
        }

        /// <summary>
        /// 根据条件获取实体
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="sort"></param>
        /// <param name="projection"></param>
        /// <param name="hint">hint索引</param>
        /// <param name="readConcern">访问设置</param>
        /// <returns></returns>
        public TEntity Get(FilterDefinition<TEntity> filter
            , ProjectionDefinition<TEntity, TEntity> projection = null
            , SortDefinition<TEntity> sort = null, BsonValue hint = null
            , ReadConcern readConcern = null)
        {
            var option = base.CreateFindOptions(projection, sort, limit: 1, hint: hint);
            var result = base.GetCollection(readConcern).FindSync(filter, option);
            var reslut = result.ToList();

            return reslut.FirstOrDefault();
        }

        /// <summary>
        /// 根据条件获取获取列表
        /// </summary>
        /// <param name="filterExp">查询条件表达式</param>
        /// <param name="includeFieldExp">查询字段表达式</param>
        /// <param name="sortExp">排序表达式</param>
        /// <param name="sortType">排序方式</param>
        /// <param name="limit"></param>
        /// <param name="skip"></param>
        /// <param name="hint">hint索引</param>
        /// <param name="readConcern">访问设置</param>
        /// <returns></returns>
        public List<TEntity> GetList(Expression<Func<TEntity, bool>> filterExp = null
            , Expression<Func<TEntity, object>> includeFieldExp = null
            , Expression<Func<TEntity, object>> sortExp = null, SortType sortType = SortType.Ascending
            , int limit = 0, int skip = 0, BsonValue hint = null
            , ReadConcern readConcern = null)
        {
            FilterDefinition<TEntity> filter = null;
            ProjectionDefinition<TEntity, TEntity> projection = null;
            SortDefinition<TEntity> sort = null;

            if (filterExp != null)
            {
                filter = Builders<TEntity>.Filter.Where(filterExp);
            }
            else
            {
                filter = Builders<TEntity>.Filter.Empty;
            }

            sort = base.CreateSortDefinition(sortExp, sortType);

            if (includeFieldExp != null)
            {
                projection = base.IncludeFields(includeFieldExp);
            }
            var option = base.CreateFindOptions(projection, sort, limit, skip, hint: hint);
            var result = base.GetCollection(readConcern).FindSync(filter, option);
            var reslut = result.ToList();

            return reslut;
        }

        /// <summary>
        /// 根据条件获取获取列表
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="projection"></param>
        /// <param name="sort"></param>
        /// <param name="limit"></param>
        /// <param name="skip"></param>
        /// <param name="hint">hint索引</param>
        /// <param name="readConcern">访问设置</param>
        /// <returns></returns>
        public List<TEntity> GetList(FilterDefinition<TEntity> filter
            , ProjectionDefinition<TEntity, TEntity> projection = null
            , SortDefinition<TEntity> sort = null
            , int limit = 0, int skip = 0, BsonValue hint = null
            , ReadConcern readConcern = null)
        {
            var option = base.CreateFindOptions(projection, sort, limit, skip, hint: hint);
            var result = base.GetCollection(readConcern).FindSync(filter, option);
            var reslut = result.ToList();

            return reslut;
        }

        /// <summary>
        /// 数量
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="limit"></param>
        /// <param name="skip"></param>
        /// <param name="hint">hint索引</param>
        /// <param name="readConcern">访问设置</param>
        /// <returns></returns>
        public long Count(FilterDefinition<TEntity> filter
            , int limit = 0, int skip = 0, BsonValue hint = null
            , ReadConcern readConcern = null)
        {
            var option = base.CreateCountOptions(limit, skip, hint);

            return base.GetCollection(readConcern).Count(filter, option);
        }

        /// <summary>
        /// 数量
        /// </summary>
        /// <param name="filterExp"></param>
        /// <param name="limit"></param>
        /// <param name="skip"></param>
        /// <param name="hint">hint索引</param>
        /// <param name="readConcern">访问设置</param>
        /// <returns></returns>
        public long Count(Expression<Func<TEntity, bool>> filterExp
            , int limit = 0, int skip = 0, BsonValue hint = null
            , ReadConcern readConcern = null)
        {
            var option = base.CreateCountOptions(limit, skip, hint);

            return base.GetCollection(readConcern).Count(filterExp, option);
        }

        /// <summary>
        /// 数量
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="hint">hint索引</param>
        /// <param name="readConcern">访问设置</param>
        /// <returns></returns>
        public bool Exists(FilterDefinition<TEntity> filter
            , BsonValue hint = null
            , ReadConcern readConcern = null)
        {
            var option = base.CreateCountOptions(1, 0, hint);

            return base.GetCollection(readConcern).Count(filter, option) > 0;
        }

        /// <summary>
        /// 数量
        /// </summary>
        /// <param name="filterExp"></param>
        /// <param name="hint">hint索引</param>
        /// <param name="readConcern">访问设置</param>
        /// <returns></returns>
        public bool Exists(Expression<Func<TEntity, bool>> filterExp
            , BsonValue hint = null
            , ReadConcern readConcern = null)
        {
            var option = base.CreateCountOptions(1, 0, hint);

            return base.GetCollection(readConcern).Count(filterExp, option) > 0;
        }
    }
}
