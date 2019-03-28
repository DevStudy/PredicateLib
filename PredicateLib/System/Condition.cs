﻿using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace System
{
    /// <summary>
    /// 表示查询条件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Condition<T>
    {
        /// <summary>
        /// 查询条件的值
        /// </summary>
        private readonly List<ConditionItem<T>> conditionItems = new List<ConditionItem<T>>();

        /// <summary>
        /// 获取T类型的所有属性
        /// </summary>
        public readonly static PropertyInfo[] TypeProperties = typeof(T).GetProperties();

        /// <summary>
        /// 查询条件
        /// </summary>
        /// <param name="conditionItems">查询条件项</param>
        public Condition(IEnumerable<KeyValuePair<string, string>> conditionItems)
            : this(GetConditionItems(conditionItems))
        {
        }

        /// <summary>
        /// 查询条件
        /// </summary>
        /// <param name="conditionValues">查询条件项</param>
        public Condition(IEnumerable<KeyValuePair<string, object>> conditionItems)
            : this(GetConditionItems(conditionItems))
        {
        }

        /// <summary>
        /// 查询条件
        /// </summary>
        /// <param name="conditionItems">查询条件项</param>
        public Condition(IEnumerable<ConditionItem<T>> conditionItems)
        {
            if (conditionItems == null)
            {
                return;
            }
            this.conditionItems.AddRange(conditionItems);
        }

        /// <summary>
        /// 转换条件值
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="keyValues">条件值</param>
        /// <returns></returns>
        private static IEnumerable<ConditionItem<T>> GetConditionItems<TValue>(IEnumerable<KeyValuePair<string, TValue>> keyValues)
        {
            if (keyValues == null)
            {
                yield break;
            }

            foreach (var keyValue in keyValues)
            {
                var member = TypeProperties.FirstOrDefault(item => item.Name.Equals(keyValue.Key, StringComparison.OrdinalIgnoreCase));
                if (member != null)
                {
                    yield return new ConditionItem<T>(member, keyValue.Value);
                }
            }
        }

        /// <summary>
        /// 配置忽略的条件
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="keySelector">属性选择</param>
        /// <returns></returns>
        public Condition<T> IgnoreFor<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            var exp = keySelector.Body as MemberExpression;
            var targets = this.conditionItems.Where(item => item.Member == exp.Member).ToArray();
            foreach (var item in targets)
            {
                this.conditionItems.Remove(item);
            }
            return this;
        }

        /// <summary>
        /// 配置比较操作符
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="keySelector">属性选择</param>
        /// <param name="operator">操作符</param>
        /// <returns></returns>
        public Condition<T> OperatorFor<TKey>(Expression<Func<T, TKey>> keySelector, Operator @operator)
        {
            var exp = keySelector.Body as MemberExpression;
            var targets = this.conditionItems.Where(item => item.Member == exp.Member);
            foreach (var item in targets)
            {
                item.Operator = @operator;
            }
            return this;
        }

        /// <summary>
        /// 转换为And连接的谓词筛选表达式
        /// </summary>
        /// <returns></returns>
        public Expression<Func<T, bool>> ToAndPredicate()
        {
            if (this.conditionItems.Count == 0)
            {
                return Predicate.True<T>();
            }

            return this.conditionItems
                .Select(item => item.ToPredicate())
                .Aggregate((left, right) => left.And(right));
        }

        /// <summary>
        /// 转换为Or连接的谓词筛选表达式
        /// </summary>
        /// <returns></returns>
        public Expression<Func<T, bool>> ToOrPredicate()
        {
            if (this.conditionItems.Count == 0)
            {
                return Predicate.False<T>();
            }

            return this.conditionItems
                .Select(item => item.ToPredicate())
                .Aggregate((left, right) => left.Or(right));
        }
    }
}
