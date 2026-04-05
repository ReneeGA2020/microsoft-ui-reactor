namespace DuctOutlook.Models;

public static class MockData
{
    public static MailFolder[] GetFolders()
    {
        const string acct = "personal";
        return
        [
            new("inbox", "Inbox", "\uE715", 155, null, acct, true, null),
            new("sent", "Sent Items", "\uE724", 0, null, acct, true, null),
            new("drafts", "Drafts", "\uE70B", 2, null, acct, true, null),
            new("deleted", "Deleted Items", "\uE74D", 0, null, acct, true, null),
            new("archive", "Archive", "\uE7B8", 0, null, acct, false, null),
            new("junk", "Junk Email", "\uE10A", 5, null, acct, false, null),
            new("notes", "Notes", "\uE70B", 0, null, acct, false, null),
        ];
    }

    public static EmailMessage[] GetMessages(string folderId)
    {
        if (folderId != "inbox") return [];

        var today = DateTimeOffset.Now.Date;
        var yesterday = today.AddDays(-1);

        return
        [
            new("m1", "Logan Iyer", "logan@outlook.com", "LI",
                "Outlook on Windows",
                "Quick update on the release timeline for the desktop client...",
                WrapHtml("<h2>Outlook on Windows</h2><p>Hi team,</p><p>Quick update on the release timeline for the desktop client. We're targeting the end of Q2 for the next major milestone. Key areas of focus:</p><ul><li>Performance improvements in calendar rendering</li><li>New compose experience</li><li>Accessibility audit fixes</li></ul><p>Let me know if you have questions.</p><p>Thanks,<br/>Logan</p>"),
                new DateTimeOffset(today, TimeSpan.Zero).AddHours(9).AddMinutes(4),
                false, false, false, false,
                MessageImportance.Normal, MessageTab.Focused, "inbox",
                ["chris@outlook.com"]),

            new("m2", "Shen Chauhan", "shen.c@gmail.com", "SC",
                "Aligning on Scope and Accountability",
                "I've aligned with DPA should own the dev experience charter since we are making good progress...",
                WrapHtml("<p>Hi Chris,</p><p>I've aligned with DPA — they should own the dev experience charter since we are making good progress on the core platform work. Let's sync on Thursday to finalize the scope document.</p><p>Best,<br/>Shen</p>"),
                new DateTimeOffset(yesterday, TimeSpan.Zero).AddHours(10).AddMinutes(56),
                false, false, false, false,
                MessageImportance.High, MessageTab.Focused, "inbox",
                ["chris@outlook.com", "team@outlook.com"]),

            new("m3", "Outlook", "noreply@outlook.com", "O",
                "Reaction Daily Digest - Friday, April 3, 2026",
                "Manoj Kumar reacted to your message on 4/3/2026...",
                WrapHtml("<h3>Reaction Daily Digest</h3><p><strong>Manoj Kumar</strong> reacted to your message on 4/3/2026 5:03 PM</p><p>Re: Exploring Windows #2</p><blockquote>Rajas — what will become the public-facing repo?</blockquote><p><em>👍 from Manoj Kumar</em></p><hr/><p><strong>Microsoft</strong><br/>Cross-Datamart<br/>You recently joined Microsoft!</p>"),
                new DateTimeOffset(yesterday, TimeSpan.Zero).AddHours(10).AddMinutes(22),
                true, false, false, false,
                MessageImportance.Normal, MessageTab.Focused, "inbox",
                ["chris@outlook.com"]),

            new("m4", "Ramraj Balasubramanian", "ramraj@outlook.com", "RB",
                "Ramraj Balasubramanian sent a message in Teams",
                "Justin Wade: it's a tall for both query and ingest but local idea is to think of it as a L...",
                WrapHtml("<p><em>Message from Teams</em></p><p><strong>Ramraj Balasubramanian</strong> sent a message:</p><p>Justin Wade: it's a tall for both query and ingest but the local idea is to think of it as a layered approach. We can start with the simple case and iterate.</p>"),
                new DateTimeOffset(yesterday, TimeSpan.Zero).AddHours(6).AddMinutes(44),
                true, false, false, false,
                MessageImportance.Normal, MessageTab.Focused, "inbox",
                ["chris@outlook.com"]),

            new("m5", "WinLearn", "winlearn@microsoft.com", "WL",
                "[WinLearn] AltWork: Learn, Apply, Accelerate",
                "A monthly spotlight on how employees are using AI to work smarter, foster creativity, and balance...",
                WrapHtml("<h2>AltWork: Learn, Apply, Accelerate</h2><p>A monthly spotlight on how employees are using AI to work smarter, foster creativity, and balance wellbeing.</p><h3>This Month's Highlight</h3><p>See how the Edge team leveraged Copilot to reduce their bug triage time by 40%.</p><p><a href='#'>Read more →</a></p>"),
                new DateTimeOffset(yesterday, TimeSpan.Zero).AddHours(6).AddMinutes(31),
                true, false, false, true,
                MessageImportance.Normal, MessageTab.Other, "inbox",
                ["all@microsoft.com"]),

            new("m6", "WinLearn", "winlearn@microsoft.com", "WL",
                "[WinLearn] That's a Good Question",
                "Hello all! Come join us for this month's That's A Good Question! As always, if you...",
                WrapHtml("<h2>That's a Good Question</h2><p>Hello all! Come join us for this month's session. As always, if you have questions you'd like us to cover, drop them in the Teams channel ahead of time.</p><p>📅 Thursday, April 9 @ 2:00 PM PST</p><p><a href='#'>Add to calendar</a></p>"),
                new DateTimeOffset(yesterday, TimeSpan.Zero).AddHours(2).AddMinutes(29),
                true, false, false, true,
                MessageImportance.Normal, MessageTab.Other, "inbox",
                ["all@microsoft.com"]),

            new("m7", "Sarah Chen", "sarah.chen@gmail.com", "SC",
                "Weekend hiking plans",
                "Hey! Are you still up for the Mt. Si trail on Saturday? Weather looks perfect...",
                WrapHtml("<p>Hey!</p><p>Are you still up for the Mt. Si trail on Saturday? Weather looks perfect — sunny and 62°F. I was thinking we could start early, maybe 7am trailhead?</p><p>Let me know!</p><p>Sarah</p>"),
                today.AddDays(-2).AddHours(18).AddMinutes(15),
                true, false, false, false,
                MessageImportance.Normal, MessageTab.Focused, "inbox",
                ["chris@outlook.com"]),

            new("m8", "GitHub", "notifications@github.com", "GH",
                "[anthropics/claude-code] New release v1.2.0",
                "A new release has been published: claude-code v1.2.0. Notable changes include...",
                WrapHtml("<h3>anthropics/claude-code v1.2.0</h3><p>A new release has been published.</p><h4>Notable changes:</h4><ul><li>Improved context window management</li><li>New /review-pr skill</li><li>Bug fixes for Windows terminal rendering</li></ul><p><a href='#'>View release on GitHub</a></p>"),
                today.AddDays(-2).AddHours(14).AddMinutes(30),
                true, false, true, false,
                MessageImportance.Normal, MessageTab.Other, "inbox",
                ["chris@outlook.com"]),

            new("m9", "Alex Thompson", "alex.t@outlook.com", "AT",
                "Re: Code review feedback",
                "Good catch on the null check. I've pushed a fix. Also addressed the naming convention...",
                WrapHtml("<p>Good catch on the null check. I've pushed a fix. Also addressed the naming convention you flagged — the PR should be ready for another look.</p><p>Thanks,<br/>Alex</p>"),
                today.AddDays(-3).AddHours(11).AddMinutes(42),
                true, false, false, false,
                MessageImportance.Normal, MessageTab.Focused, "inbox",
                ["chris@outlook.com"]),

            new("m10", "Priya Sharma", "priya@outlook.com", "PS",
                "Q2 Planning Doc - Please Review",
                "Hi all, I've shared the Q2 planning document. Please review sections 3 and 4 by EOD Thursday...",
                WrapHtml("<p>Hi all,</p><p>I've shared the Q2 planning document. Please review <strong>sections 3 and 4</strong> by EOD Thursday. Key decision points:</p><ol><li>Resource allocation for the new rendering pipeline</li><li>Timeline for the accessibility milestone</li><li>Dependencies on the platform team</li></ol><p>Comments are open in the doc. Let's discuss in Friday's standup.</p><p>Priya</p>"),
                today.AddDays(-3).AddHours(9).AddMinutes(15),
                false, true, true, false,
                MessageImportance.High, MessageTab.Focused, "inbox",
                ["team@outlook.com"]),

            // ── Generated bulk emails for scroll testing ──────────────
            .. GenerateBulkEmails(today, 500),
        ];
    }

    static readonly string[][] FakeSenders =
    [
        ["Emily Zhang", "emily.z@outlook.com", "EZ"],
        ["Marcus Johnson", "marcus.j@gmail.com", "MJ"],
        ["Aisha Patel", "aisha.p@outlook.com", "AP"],
        ["David Kim", "david.kim@outlook.com", "DK"],
        ["Sofia Rodriguez", "sofia.r@gmail.com", "SR"],
        ["James Wilson", "james.w@outlook.com", "JW"],
        ["Nina Kowalski", "nina.k@outlook.com", "NK"],
        ["Ryan Chen", "ryan.chen@gmail.com", "RC"],
        ["Fatima Al-Hassan", "fatima@outlook.com", "FA"],
        ["Tom Anderson", "tom.a@outlook.com", "TA"],
        ["Lisa Park", "lisa.park@gmail.com", "LP"],
        ["Carlos Mendez", "carlos.m@outlook.com", "CM"],
        ["Hannah Green", "hannah.g@outlook.com", "HG"],
        ["Rajesh Kumar", "rajesh.k@outlook.com", "RK"],
        ["Olivia Brown", "olivia.b@gmail.com", "OB"],
    ];

    static readonly string[] FakeSubjects =
    [
        "Re: Weekly sync notes",
        "Build pipeline failure — need help",
        "Lunch plans for Thursday?",
        "Updated design spec v3",
        "FYI: policy change for remote work",
        "Can you review this PR?",
        "Re: Performance regression in nightly",
        "Team offsite logistics",
        "Action required: Complete compliance training",
        "Meeting notes — API design review",
        "Quick question about the config",
        "Heads up: deployment scheduled for tonight",
        "Re: Customer feedback summary",
        "New hire onboarding checklist",
        "Reminder: 1:1 at 3pm",
        "Share: Interesting article on Rust in production",
        "Bug report: crash on startup (Windows 11)",
        "Follow-up from yesterday's standup",
        "Draft: Q3 OKR proposals",
        "Invitation: Team happy hour Friday",
        "Re: Flaky test in CI — investigating",
        "Security advisory: update dependencies",
        "Your pull request was approved",
        "Sprint retrospective action items",
        "FYI: server migration this weekend",
    ];

    static readonly string[] FakePreviews =
    [
        "I looked into this and I think the root cause is...",
        "Sounds good to me. Let's sync tomorrow morning to discuss...",
        "Here's the updated version with your feedback incorporated...",
        "Just wanted to flag this before the end of the week...",
        "Thanks for the quick turnaround on this. One minor thing...",
        "I'll be out of office Thursday and Friday, so please...",
        "The numbers look promising — we're seeing a 15% improvement...",
        "Can we push this to next sprint? I think we need more...",
        "Great catch. I've filed a bug and assigned it to...",
        "Let me know if you need any help with the migration...",
    ];

    static EmailMessage[] GenerateBulkEmails(DateTime today, int count)
    {
        var rng = new Random(42); // Deterministic seed
        var result = new EmailMessage[count];
        for (int i = 0; i < count; i++)
        {
            var sender = FakeSenders[rng.Next(FakeSenders.Length)];
            var subject = FakeSubjects[rng.Next(FakeSubjects.Length)];
            var preview = FakePreviews[rng.Next(FakePreviews.Length)];
            var daysAgo = i / 8 + 4; // Spread across days, ~8 per day
            var hour = 8 + rng.Next(10);
            var minute = rng.Next(60);
            var date = today.AddDays(-daysAgo).AddHours(hour).AddMinutes(minute);
            var isRead = rng.NextDouble() > 0.3; // 70% read
            var tab = rng.NextDouble() > 0.2 ? MessageTab.Focused : MessageTab.Other;
            var hasAttachment = rng.NextDouble() > 0.85;

            result[i] = new EmailMessage(
                $"gen-{i}",
                sender[0], sender[1], sender[2],
                subject,
                preview,
                WrapHtml($"<p>{preview}</p><p>This is a generated email for scroll testing.</p><p>Best regards,<br/>{sender[0]}</p>"),
                new DateTimeOffset(date, TimeSpan.Zero),
                isRead, false, hasAttachment, false,
                MessageImportance.Normal, tab, "inbox",
                ["chris@outlook.com"]
            );
        }
        return result;
    }

    public static CalendarSource[] GetCalendarSources()
    {
        return
        [
            new("cal-work", "Calendar", "#0078D4", true),
            new("cal-personal", "Personal", "#00B294", true),
            new("cal-birthdays", "Birthdays", "#E74856", true),
        ];
    }

    public static CalendarEvent[] GetCalendarEvents(DateTimeOffset weekStart)
    {
        var mon = weekStart;
        var tue = mon.AddDays(1);
        var wed = mon.AddDays(2);
        var thu = mon.AddDays(3);
        var fri = mon.AddDays(4);

        return
        [
            // All-day events
            new("e0", "Budget OKR finalize (all morning) OKR: Martin Output",
                null, mon.AddHours(0), mon.AddDays(1), true, "cal-work", null),
            new("e0b", "Mn RSR Intention Info Dissemination",
                null, tue.AddHours(0), tue.AddDays(1), true, "cal-work", null),

            // Monday
            new("e1", "Krusty Chitchat",
                null, mon.AddHours(11), mon.AddHours(11).AddMinutes(30), false, "cal-work", null),
            new("e2", "[WinCIss] Eng / P 01",
                null, mon.AddHours(12), mon.AddHours(13), false, "cal-work", null),
            new("e3", "Multimodal AI Admin",
                null, mon.AddHours(15), mon.AddHours(16), false, "cal-work", null),
            new("e4", "W-G Partner Meeting",
                "Room Rainier",
                mon.AddHours(16), mon.AddHours(17), false, "cal-work", null),

            // Tuesday
            new("e5", "Following: AgDS\nAgenda Lunch in B1\nall BU",
                null, tue.AddHours(11).AddMinutes(30), tue.AddHours(12).AddMinutes(30), false, "cal-work", null),
            new("e6", "Consort chat (Initial)",
                null, tue.AddHours(15), tue.AddHours(16), false, "cal-work", null),
            new("e7", "Customer experience",
                null, tue.AddHours(16), tue.AddHours(17), false, "cal-personal", null),

            // Wednesday
            new("e8", "WinUX VP +",
                null, wed.AddHours(11), wed.AddHours(12), false, "cal-work", null),
            new("e9", "MCR Platform, MORE",
                null, wed.AddHours(12), wed.AddHours(13), false, "cal-work", null),
            new("e10", "WinPD Design\nReview: Basics",
                "Room Basics",
                wed.AddHours(13), wed.AddHours(14).AddMinutes(30), false, "cal-work", null),
            new("e11", "Nikola/Chris Nikola M.",
                null, wed.AddHours(13).AddMinutes(30), wed.AddHours(14), false, "cal-work", null),

            // Thursday
            new("e12", "Following: Wi-",
                null, thu.AddHours(11), thu.AddHours(12), false, "cal-work", null),
            new("e13", "Chris-Manog in-person",
                null, thu.AddHours(11).AddMinutes(30), thu.AddHours(12).AddMinutes(30), false, "cal-personal", null),
            new("e14", "Following: [Group 1]\nLow-rank adaptation\nWindows API Design",
                null, thu.AddHours(14), thu.AddHours(15), false, "cal-work", null),
            new("e15", "Tens D.\nDown UM",
                null, thu.AddHours(15), thu.AddHours(16), false, "cal-work", null),
            new("e16", "Built-\nadapt.",
                null, thu.AddHours(15).AddMinutes(30), thu.AddHours(16).AddMinutes(30), false, "cal-work", null),

            // Friday
            new("e17", "WinUX VP +",
                null, fri.AddHours(11), fri.AddHours(12), false, "cal-work", null),
            new("e18", "Chris/Chris UR (stats)",
                null, fri.AddHours(13), fri.AddHours(14), false, "cal-work", null),
            new("e19", "WinUI Perf. Resource",
                null, fri.AddHours(15).AddMinutes(30), fri.AddHours(16).AddMinutes(30), false, "cal-work", null),
            new("e20", "[Email] [Group 1] Op!",
                null, fri.AddHours(16), fri.AddHours(17).AddMinutes(30), false, "cal-work", null),

            // Late Friday event
            new("e21", "OFV U1 Daily Standup",
                null, fri.AddHours(17), fri.AddHours(17).AddMinutes(30), false, "cal-work", null),
        ];
    }

    public static DateTimeOffset GetCurrentWeekStart()
    {
        var today = DateTimeOffset.Now.Date;
        int diff = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return new DateTimeOffset(today.AddDays(-diff), TimeSpan.Zero);
    }

    private static string WrapHtml(string body) =>
        $$"""
        <!DOCTYPE html>
        <html>
        <head>
        <style>
            body { font-family: 'Segoe UI', sans-serif; font-size: 14px; color: #333; padding: 16px; margin: 0; }
            h2 { font-size: 20px; font-weight: 600; }
            h3 { font-size: 16px; font-weight: 600; }
            blockquote { border-left: 3px solid #0078D4; padding-left: 12px; color: #666; }
            a { color: #0078D4; }
            ul, ol { padding-left: 24px; }
        </style>
        </head>
        <body>{{body}}</body>
        </html>
        """;
}
