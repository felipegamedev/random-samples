using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class CostSurvey : MonoBehaviour
{
    public string patientName;
    public decimal balance;
    public decimal totalPaid;
    public List<SurveyProcedure> realizedProceduresCollection = new List<SurveyProcedure>();
    public List<SurveyProcedure> canceledProceduresCollection = new List<SurveyProcedure>();

    public event Action<decimal> OnBalanceChanged;
    public event Action<SurveyProcedure> OnRealizedProcedureAdded;
    public event Action<SurveyProcedure> OnRealizedProcedureRemoved;
    public event Action<SurveyProcedure> OnCanceledProcedureAdded;
    public event Action<SurveyProcedure> OnCanceledProcedureRemoved;

    public void AddRealizedProcedure(SurveyProcedure p_procedureToAdd)
    {
        balance -= p_procedureToAdd.GetTotalValueWithDiscount();
        realizedProceduresCollection.Add(p_procedureToAdd);
        OnBalanceChanged?.Invoke(balance);
        OnRealizedProcedureAdded(p_procedureToAdd);
    }

    public void RemoveRealizedProcedure(SurveyProcedure p_procedureToRemove)
    {
        balance += p_procedureToRemove.GetTotalValueWithDiscount();
        realizedProceduresCollection.Remove(p_procedureToRemove);
        OnBalanceChanged?.Invoke(balance);
        OnRealizedProcedureRemoved(p_procedureToRemove);
    }

    public void AddCanceledProcedure(SurveyProcedure p_procedureToAdd)
    {
        balance -= p_procedureToAdd.GetTotalPenaltyValue();
        canceledProceduresCollection.Add(p_procedureToAdd);
        OnBalanceChanged?.Invoke(balance);
        OnCanceledProcedureAdded(p_procedureToAdd);
    }

    public void RemoveCanceledProcedure(SurveyProcedure p_procedureToRemove)
    {
        balance += p_procedureToRemove.GetTotalPenaltyValue();
        canceledProceduresCollection.Remove(p_procedureToRemove);
        OnBalanceChanged?.Invoke(balance);
        OnCanceledProcedureRemoved(p_procedureToRemove);
    }

    public decimal GetTotalSpentValue()
    {
        return realizedProceduresCollection.Sum(p => p.GetTotalValueWithDiscount());
    }
    
    public decimal GetTotalPenaltyValue()
    {
        return canceledProceduresCollection.Sum(p => p.GetTotalPenaltyValue());
    }
}
