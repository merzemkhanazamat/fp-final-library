int overdue_fee(int due, int day, int rate)
{
    if (due < 0 || due > 365021 || day < 0 || day > 365000 || rate < 0 || rate > 1000)
        return -1;
    if (day <= due) return 0;
    return (day - due) * rate;
}
