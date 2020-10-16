﻿using System;
using System.Linq.Expressions;
using System.Reflection;

namespace EntityTable.Extensions
{
    public static class ExpressionExtensions
    {
        public static MemberInfo GetMemberInfo<T, U>(this Expression<Func<T, U>> expression)
        {
            MemberExpression member = expression.Body as MemberExpression;
            if (member == null)
            {
                // The property access might be getting converted to object to match the func
                // If so, get the operand and see if that's a member expression
                member = (expression.Body as UnaryExpression)?.Operand as MemberExpression;
            }
            if (member == null)
            {
                //Action must be a member expression.
                return null;
            }
            return member.Member;
        }

        public static PropertyInfo GetPropertyInfo<T, U>(this Expression<Func<T, U>> expression)
        {
            MemberExpression member = expression.Body as MemberExpression;
            if (member == null)
            {
                // The property access might be getting converted to object to match the func
                // If so, get the operand and see if that's a member expression
                member = (expression.Body as UnaryExpression)?.Operand as MemberExpression;
            }
            if (member == null)
            {
                //Action must be a member expression
                return null;
            }
            if (member.Member is PropertyInfo)
                return member.Member as PropertyInfo;

            //Expression member is not a property
            return null;
        }
    }
}