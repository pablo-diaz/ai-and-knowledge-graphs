namespace Common;

public class UserQuestionRetriever
{
    public delegate string GetQuestionFromUserInput(string withUserPrompt);

    public static string[] GetQuestionsFromUser(string maybeUserQuestionProvided, GetQuestionFromUserInput fnGetQuestionFromUserInput)
    {
        if(false == string.IsNullOrWhiteSpace(maybeUserQuestionProvided)) return [maybeUserQuestionProvided];

        var maybeUserQuestion = fnGetQuestionFromUserInput(withUserPrompt:
            "Please type your question (or leave it empty to run default questions)");

        return false == string.IsNullOrWhiteSpace(maybeUserQuestion)
            ? [maybeUserQuestion]
            : [
                "Which are the top 10 Customers that have submitted the most Orders for Laboratory 55?",
                "Which are the top 20 most important category A Customers for lab 01?",
                "Which are the Customers in lab 55 which have category A or B? and which other customers these are 'similar to', inside that same laboratory? Please return their names and categories",
                "Which are the top 5 sell representatives, for lab 55? please return their names and the amount of orders sold for each customer they attended"
            ];
    }

}
