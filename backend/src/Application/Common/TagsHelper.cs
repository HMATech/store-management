using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Common;

public static class TagsHelper
{
    public async static Task<IEnumerable<T>> CreateSynchronizedTagsAsync<T>(string[] inputTagStrings,
        Func<string, T> tagFactory, IEnumerable<T> entityTags, DbSet<T> tagsTable,
        CancellationToken cancellationToken) where T : Tag
    {
        var tags = new List<T>();

        var inputTags = inputTagStrings.Distinct().Select(tagFactory);

        bool entityTagsHasAny = entityTags.Any();

        var newTags = entityTagsHasAny ?
            inputTags.ExceptBy(entityTags.Select(t => t.Name.Normalized), t => t.Name.Normalized) :
            inputTags;

        if (entityTagsHasAny)
        {
            var persistingTags = entityTags.IntersectBy(inputTags.Select(t => t.Name.Normalized),
                t => t.Name.Normalized);

            tags.AddRange(persistingTags);
        }

        var normalizedNewTags = newTags.Select(t => t.Name.Normalized);
        var existingNewTags = await tagsTable
            .Where(t => normalizedNewTags.Contains(t.Name.Normalized))
            .ToArrayAsync(cancellationToken);

        newTags = newTags.ExceptBy(existingNewTags.Select(t => t.Name.Normalized), t => t.Name.Normalized);

        tags.AddRange(existingNewTags);
        tags.AddRange(newTags);

        return tags;
    }
}