using Newtonsoft.Json;
using System.Linq.Expressions;

namespace CMMS.API.Helpers
{
    public static class LinqHelpers
    {
        public static IQueryable<T> ToPageList<T>(this IQueryable<T> src, int curPage, int perPage)
        {
            return src.Skip(curPage * perPage).Take(perPage);
        }

        public static IEnumerable<T> ToPageList<T>(this IEnumerable<T> src, int curPage, int perPage)
        {
            return src.Skip(curPage * perPage).Take(perPage);
        }

        public static IList<T> ToPageList<T>(this IList<T> src, int curPage, int perPage)
        {
            return src.Skip(curPage * perPage).Take(perPage).ToList();
        }

        public static IQueryable<T> Sort<T>(this IQueryable<T> source, string sortBy, bool isAscending)
        {
            if (String.IsNullOrWhiteSpace(sortBy))
            {
                return source;
            }
            var param = Expression.Parameter(typeof(T), "item");

            var sortExpression = Expression.Lambda<Func<T, object>>
                (Expression.Convert(Expression.Property(param, sortBy), typeof(object)), param);

            if (isAscending)
            {
                return source.OrderBy<T, object>(sortExpression);
            }
            return source.OrderByDescending<T, object>(sortExpression);
        }

        //public static IEnumerable<T> GroupBy<T, Tkey>(this IEnumerable<T> source, Expression<Func<T,Tkey>> keySelector)
        //{
        //    IDictionary<Tkey, T> map = new Dictionary<Tkey, T>();

        //}

        public static T Clone<T>(this T source)
        {
            var serialized = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(serialized);
        }

        public static string ValueToString(this Enum source)
        {
            return Convert.ToInt32(source).ToString();
        }

        public static IEnumerable<T> LeftJoin<T>(this IEnumerable<T> firstList, IEnumerable<T> secondList)
        {
            var query = from first in firstList
                        join second in secondList on first equals second into result
                        from item in result.DefaultIfEmpty()
                        select item;
            return query;
        }
        public static IQueryable<T> FilterByCategories<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
        {
            return source.Where(predicate);
        }
    }
}
