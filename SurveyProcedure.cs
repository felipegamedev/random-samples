using System.Collections.Generic;

public class SurveyProcedure
{
    public int number;
    public string id;
    public string name;
    public int count;
    public List<string> toothArcadeCollection;
    public decimal totalValue;
    public decimal discountPercentage;
    public decimal cancelationPenaltyPercentage;

    public decimal GetTotalValueWithDiscount()
    {
        return totalValue * (1 - discountPercentage / 100);
    }

    public decimal GetTotalPenaltyValue()
    {
        return totalValue * cancelationPenaltyPercentage / 100;
    }
}
