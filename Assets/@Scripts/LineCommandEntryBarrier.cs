using System.Collections.Generic;

public sealed class LineCommandEntryBarrier
{
    private readonly List<CommandRunTicket> _tickets = new ();

    public int TicketCount
    {
        get { return _tickets.Count; }
    }

    public bool IsEntryClosed
    {
        get
        {
            for (int i = 0; i < _tickets.Count; i++)
            {
                CommandRunTicket ticket = _tickets[i];

                if (ticket != null && !ticket.EntryClosed)
                    return false;
            }

            return true;
        }
    }

    public bool IsEntrySatisfied
    {
        get
        {
            for (int i = 0; i < _tickets.Count; i++)
            {
                CommandRunTicket ticket = _tickets[i];

                if (ticket != null && !ticket.EntrySatisfied)
                    return false;
            }

            return true;
        }
    }

    public void Clear()
    {
        _tickets.Clear();
    }

    public void Register(CommandRunTicket ticket)
    {
        if (ticket == null)
            return;

        _tickets.Add(ticket);
    }
}