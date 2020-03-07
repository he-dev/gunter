using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Custom;
using Reusable;

namespace Gunter.Data.Abstractions
{
    public abstract class Union<T> : IModel where T : IModel
    {
        protected Union(T model, IEnumerable<Specification> templates)
        {
            Model = model;
            Templates = templates;
        }

        private T Model { get; }

        private IEnumerable<Specification> Templates { get; }

        public SoftString Id => Model.Id;

        public Merge? Merge => Model.Merge;

        protected TResult GetValue<TResult>(Func<T, TResult> selector, Func<TResult, bool> isValid)
        {
            return
                selector(Model) is {} value && isValid(value)
                    ? value
                    : selector(FindTemplateModel());
        }

        private T FindTemplateModel()
        {
            return
                Templates.SingleOrDefault(p => p.Name == Merge.Name) is {} template
                    ? template.Flatten().OfType<T>().SingleOrDefault(p => p.Id == Merge.Id)
                    : default;
        }
    }
}