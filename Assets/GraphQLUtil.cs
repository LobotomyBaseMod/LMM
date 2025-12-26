using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

public class GraphQLUtil
{
    public static string GetNexusRequestQuery(string keyword = "", int page = 0)
    {
        var requestPayload = new
        {
            query = @"
            query ModsListing($count: Int = 0, $facets: ModsFacet, $filter: ModsFilter, $offset: Int, $postFilter: ModsFilter, $sort: [ModsSort!]) {
                mods(
                    count: $count
                    facets: $facets
                    filter: $filter
                    offset: $offset
                    postFilter: $postFilter
                    sort: $sort
                    viewUserBlockedContent: false
                ) {
                    facetsData
                    nodes {
                        ...ModFragment
                    }
                    totalCount
                }
            }
            fragment ModFragment on Mod {
                adultContent
                createdAt
                downloads
                endorsements
                fileSize
                game {
                    domainName
                    id
                    name
                }
                modCategory {
                    categoryId
                    name
                }
                modId
                name
                status
                summary
                thumbnailUrl
                uid
                updatedAt
                uploader {
                    avatar
                    memberId
                    name
                }
            }",
            variables = new
            {
                count = 20,
                facets = new { categoryName = new string[] { }, languageName = new string[] { }, tag = new string[] { } },
                filter = new
                {
                    gameDomainName = new[] { new { op = "EQUALS", value = "lobotomycorporation" } },
                    description = new[] { new { op = "MATCHES", value = keyword } }
                },
                offset = (page-1)*20,
                postFilter = new { },
                sort = new
                {
                    createdAt = new { direction = "DESC" }
                }
            },
            operationName = "ModsListing"
        };

        var result = JsonConvert.SerializeObject(requestPayload);
        return result;
    }
}