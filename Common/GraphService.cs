namespace Common;

public class GraphService
{
    public sealed class SuggestedQuery
    {
        [Newtonsoft.Json.JsonProperty("cypherQuery")]
        public string Query { get; set; }
    }

    public static string BuildUserMessageToGetCypherQuery(string fromUserQuestion) => $"""
        Now please provide the cypher query that can answer the following question:
        <question>
        {fromUserQuestion}
        </question>
        
        Please, do not explain your reasoning, only answer with the cypher query that should be run. Thank you
        """;
    
    public static string BuildUserMessageToEnhanceAnswer(string toUserQuestion, string usingTableInformation) => $"""
        User asked the following question:
        <question>
        {toUserQuestion}
        </question>
        
        And we have the following information (from the database) that can be used to answer that question:
        <information>
        {usingTableInformation}
        </information>

        Please, provide a complete answer to the user question, using the information provided.
        Please DO NOT invent any information that is not included in the provided information. Thank you!
        """;

    public static string CreateJsonStructuredOutputSchemaForSuggestedQueryDTO() => """
    {
        "type": "object",
        "properties": {
            "cypherQuery": { "type": "string" }
        },
        "required": ["cypherQuery"]
    }
    """;

    public static string GetSystemPromptToGenerateCypherQueriesFromGraphSchema() => $"""
    You are a helpful assistant that knows how to run cypher queries in a neo4j database. The following is the schema of that graph loaded in a graph:

    ```markdown
    {GetSchemaOfTheGraph()}
    ```

    Here it is an example of a cypher query of that graph, which returns the top 30 most important Tests that have been conducted gloablly:

    ```cypher
    match (a:Analysis)-[:of]->(t:Test)
    return t.name as testName, count(*) as analysisCountForTest
    order by analysisCountForTest desc
    limit 30
    ```

    And here is another example cypher query that answer the question of: "Top 10 of the biggest orders", according to their sample count:

    ```cypher
    MATCH (o:Order)-[:has]->(s:Sample)
    RETURN o.id as orderId, COUNT(s) AS sampleCount
    ORDER BY sampleCount DESC
    LIMIT 10
    ```
    """;

    private static string GetSchemaOfTheGraph() => """
    # List of Nodes

    ## Category

    ### Purpose
    It is a mean to categorize customers by their importance

    ### Properties

    #### name
    - Type: string
    - Purpose: Name of the Category. "A" is the most important category, then "B", then "C", etc... At the end, "E" is the least important one

    ## Company

    ### Purpose
    Know which Customers are bound to a specific Company

    ### Properties

    #### name
    - Type: string
    - Purpose: Name of the Company

    ## CustomerType

    ### Purpose
    Categorize each Customer by its Type

    ### Properties

    #### name
    - Type: string
    - Purpose: Name of the Type of Customer

    ## Customer

    ### Purpose
    Info about each Customer

    ### Properties

    #### id
    - Type: string
    - Purpose: Id of the Customer

    ## Contact

    ### Purpose
    Info about Contacts that each Customer has

    ### Properties

    #### name
    - Type: string
    - Purpose: Name of the Contact of the Customer

    ## Project

    ### Purpose
    Info about Projects that each Customer runs

    ### Properties

    #### name
    - Type: string
    - Purpose: Name of the Project that a Customer runs

    ## ShippingMethod

    ### Purpose
    Info about Shipping Methods used to send results to Customers about Orders they have submitted

    ### Properties

    #### name
    - Type: string
    - Purpose: Name of the Shipping Method used

    ## Order

    ### Purpose
    Info about each Order that a Customer has submitted

    ### Properties

    #### id
    - Type: string
    - Purpose: Id of the Order

    #### name
    - Type: string
    - Purpose: Friendly name of the Order

    ## Sample

    ### Purpose
    Info about each Sample that is part of an Order that a Customer has submitted

    ### Properties

    #### id
    - Type: string
    - Purpose: Id of the sample

    #### name
    - Type: string
    - Purpose: friendly name of the sample

    ## Laboratory

    ### Purpose
    Info about each Laboratory that provides services to Customers by analyzing their submitted Orders

    ### Properties

    #### id
    - Type: string
    - Purpose: Id of the Laboratory

    #### name
    - Type: string
    - Purpose: Short name of the Laboratory

    #### longerName
    - Type: string
    - Purpose: Full name of the Laboratory

    ## City

    ### Purpose
    Info about Cities in States of Countries of the World

    ### Properties

    #### name
    - Type: string
    - Purpose: name of the city

    ## State

    ### Purpose
    Info about States of Countries of the World

    ### Properties

    #### name
    - Type: string
    - Purpose: name of the state

    ## Country

    ### Purpose
    Info about Countries of the World

    ### Properties

    #### name
    - Type: string
    - Purpose: name of the country

    ## Matrix

    ### Purpose
    Info about Matrices that a Sample of an Order has been submitted as. They are a type of material that each sample is made of

    ### Properties

    #### name
    - Type: string
    - Purpose: name of the matrix

    ## Analysis

    ### Purpose
    Info about Analyzes that have been planned (and maybe done at a specific Laboratory) for each Sample of an Order that a Customer has submitted

    ### Properties

    #### id
    - Type: string
    - Purpose: id of the Analysis

    #### name
    - Type: string
    - Purpose: name of the Analysis

    ## Method

    ### Purpose
    Info about Methods that are followed when conducting (performing) an Analysis of a Sample in a Laboratory

    ### Properties

    #### name
    - Type: string
    - Purpose: name of the method

    ## Test

    ### Purpose
    Info about type of Tests that are planned (and maybe done) for Samples

    ### Properties

    #### name
    - Type: string
    - Purpose: name of the test

    #### sequenceNumber
    - Type: string
    - Purpose: unique identifier of the test

    ## Employee

    ### Purpose
    Info about Employees that work in a Laboratoy, analyzing samples

    ### Properties

    #### id
    - Type: string
    - Purpose: name of the employee

    # List of Relationships between these Nodes

    - Know a CustomerType of a given Customer
    	- From node: Customer
    	- To node: CustomerType
    	- Relationship name: of

    - Know the Company linked a given Customer
    	- From node: Customer
    	- To node: Company
    	- Relationship name: of

    - Know how customers are similar to other customers, in different scopes of similarity, which is based on the similarity of tests conducted for orders submitted by each customer
    	- From node: Customer
    	- To node: Customer
    	- Relationship name: similar_to
    	- Relationship properties:
    		- scope: The scope of the relationship. For example: "Laboratory" means that a Customer is similar to another Customer, in the scope of a given Laboratory
    		- for_lab: present when the scope is "Laboratory". It points to the ID of the Laboratory where this similarity is scoped for
    		- score: regardless the scope, it is a decimal number from 0.0 to 1.0, which means the level of similarity, being 1.0 the highest degree of similarity and 0.0 the lowest

    - Know how customers are categorized in their level of importance, where "A" is the highest category for customers that are very important (due to the amount of orders submitted), whereas "E" is the least important category
    	- From node: Customer
    	- To node: Category
    	- Relationship name: categorized_as
    	- Relationship properties:
    		- scope: The scope of the relationship. For example:
    			- Laboratory: means that a Customer is categorized with a specific category, in the scope of a given Laboratory, taking the amount of orders submitted at that specific laboratory
    			- Global: means that it si a global categorization of the customer, taking the amount of orders submitted at different laboratories
    		- for_lab: present when the scope is "Laboratory". It points to the ID of the Laboratory where this categorization is scoped for

    - Know the Customer of a given Contact
    	- From node: Contact
    	- To node: Customer
    	- Relationship name: of

    - Know the Contact that an Order has been placed for
    	- From node: Order
    	- To node: Contact
    	- Relationship name: for

    - Know the Contact that an Order has been invoiced for
    	- From node: Order
    	- To node: Contact
    	- Relationship name: invoiced
    	- This is an optional relationship, because some orders might not have been invoiced yet
    	- It could also point to the same Contact the order was placed for

    - Know the Customer Project that an Order has been set to
    	- From node: Order
    	- To node: Project
    	- Relationship name: for
    	- This is an optional relationship, because some orders might not have been set to a Project

    - Know the Shipping Method that an Order has been set to
    	- From node: Order
    	- To node: ShippingMethod
    	- Relationship name: with
    	- This is an optional relationship, because some orders might not have been set to a Shipping Method yet

    - Know the Laboratory that an Order has been submitted to
    	- From node: Order
    	- To node: Laboratory
    	- Relationship name: logged_in_at

    - Know the City of a given Laboratory
    	- From node: Laboratory
    	- To node: City
    	- Relationship name: in

    - Know the State (aka Province) of a given City
    	- From node: City
    	- To node: State
    	- Relationship name: in

    - Know the Country of a given State (aka Province)
    	- From node: State
    	- To node: Country
    	- Relationship name: in

    - Know which set of Laboratories have been SubContracted (to run Analyzes) by a given Laboratory
    	- From node: Laboratory
    	- To node: Laboratory
    	- Relationship name: subcontracts
    	- Relationship properties:
    		- scope: The subcontracting purpose. For example: "For Analyzes" means that subcontracting was done to run analyzes in the subcontracted laboratory
    		- category: the level of importance of a subcontracted lab, "A" being the most important, then "B", "C" and so on, and "E" being the least important

    - Know the Sample set of a given Order submitted by a Customer
    	- From node: Order
    	- To node: Sample
    	- Relationship name: has

    - Know the Matrix of a given Sample of an Order
    	- From node: Sample
    	- To node: Matrix
    	- Relationship name: of

    - Know the Analysis set planned (and probably done) for a given Sample of an Order
    	- From node: Sample
    	- To node: Analysis
    	- Relationship name: has

    - Know the Method that was followed by a given Analysis of a Sample (at the time it was analyzed)
    	- From node: Analysis
    	- To node: Method
    	- Relationship name: following

    - Know the Test name that an Analysis of a Sample was set to (or was marketed as)
    	- From node: Analysis
    	- To node: Test
    	- Relationship name: of

    - Know the set of Tests that are part of a Group Test
    	- From node: Test
    	- To node: Test
    	- Relationship name: part_of
    	- This relationship might not exist for some tests that are not part of any group test

    - Know in which Laboratory a given Analysis was created at
    	- From node: Analysis
    	- To node: Laboratory
    	- Relationship name: logged_in_at

    - Know in which Laboratory a given Analysis was performed (conducted) at
    	- From node: Analysis
    	- To node: Laboratory
    	- Relationship name: analyzed_at
    	- This relationship might not exist when the given analysis has not been performed (conducted) yet.

    - Know who sold a given Order to a Contact of a Customer
    	- From node: Order
    	- To node: Employee
    	- Relationship name: sold_by
    	- In this relationship, the employee can be considered as the "Sales Representative" of that Customer

    - Know who created a given Order in a Laboratory
    	- From node: Order
    	- To node: Employee
    	- Relationship name: logged_in_by
    	- In this relationship, the employee can be considered as the "Login Person" that created that order in that laboratory

    - Know who signed of the all the analyzes performed (conducted) to given Order
    	- From node: Order
    	- To node: Employee
    	- Relationship name: signed_off_by
    	- In this relationship, the employee can be considered as the "Analyst" that signs off that order in that laboratory

    - Know who performed (conducted) a given Analysis planned for a Sample
    	- From node: Analysis
    	- To node: Employee
    	- Relationship name: analyzed_by
    	- In this relationship, the employee can be considered as the "Analyst" that performed (conducted) the analysis planned for that sample in that laboratory

    - Know who performed (conducted) the Preparation of a given Sample
    	- From node: Sample
    	- To node: Employee
    	- Relationship name: preped_by
    	- When this relationship exists, the target employee can be considered as the "Prep Technician" that performed (conducted) the Preparation of that Sample in that laboratory, before its planned analyzes were sent to be analyzed

    - Know which Preparation Method was followed when the Preparation of a given Sample was done
    	- From node: Sample
    	- To node: Method
    	- Relationship name: preped_following
    	- This is an optional relationship
    """;

}
