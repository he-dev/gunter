using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Custom;
using Reusable;

namespace Gunter.Data.Abstractions
{
    public abstract class Union<T> : IModel where T : IModel, IMergeable
    {
        protected Union(T model, IEnumerable<TheoryFile> templates)
        {
            Model = model;
            Templates = templates;
        }

        protected T Model { get; }

        protected IEnumerable<TheoryFile> Templates { get; }

        public SoftString Name => Model.Name;

        public List<TemplateSelector>? TemplateSelectors => Model.TemplateSelectors;

        protected TResult GetValue<TResult>(Func<T, TResult> selector, Func<TResult, bool> isValid)
        {
            return selector(Model) is {} value && isValid(value) ? value : FindTemplateModels().Select(selector).FirstOrDefault(isValid);
        }

        //protected IEnumerable<TProperty> Merge<TProperty>(Func<T, TProperty> selector)

        private IEnumerable<T> FindTemplateModels()
        {
            var models =
                from selector in TemplateSelectors
                from template in Templates
                where selector.TemplateName is null || selector.TemplateName.Equals(template.Name)
                from model in template
                where model is T && (selector.ModelId is null || selector.ModelId.Equals(model.Name))
                select model;

            return models.Cast<T>();
        }
    }
}