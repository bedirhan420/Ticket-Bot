#region Usings
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
#endregion

#region Classes
internal class Member
{
    public string userId = "";
    public string userName = ""; 
    public DateTime cooldown { get; set; }

    public List<Ticket> tickets = new List<Ticket>();

    public static void SaveAllToJson(List<Member> members, string filePath)
    {
        try
        {
            string json = JsonConvert.SerializeObject(members, Formatting.Indented);
            File.WriteAllText(filePath, json);
            Console.WriteLine($"Members saved to {filePath} successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving members to {filePath}: {ex.Message}");
        }
    }

}
internal class Ticket
{
   
    public ulong id=0 ;
    //public RestTextChannel channel { get; set; }
    public int rate = 1;
    public string experience = "";
    public string aim = "";
    public string topic = "";
    public string question = "";
    public string adminsUserId = "";
    public Ticket()
    {
        string hexGuid = Guid.NewGuid().ToString("N").Substring(0, 16);
        // Ensure that the string is not too long for ulong.Parse
        if (hexGuid.Length > 16)
        {
            hexGuid = hexGuid.Substring(0, 16);
        }

        id = ulong.Parse(hexGuid, System.Globalization.NumberStyles.HexNumber);
    }
}
internal class Admin
{
    public string userId = "";
    public int totalStarCount = 0;
    public List<string> feedbackmsgs = new List<string>(); 
    public List<ulong> ticketIds = new List<ulong>();
    public static void SaveAllToJson(List<Admin> admins, string filePath)
    {
        try
        {
            string json = JsonConvert.SerializeObject(admins, Formatting.Indented);
            File.WriteAllText(filePath, json);
            Console.WriteLine($"Members saved to {filePath} successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving members to {filePath}: {ex.Message}");
        }
    }
}
#endregion

namespace ticketbot
{
    internal class Program
    {
        #region Variables
        public static DiscordSocketClient _client;
        public static string botToken = "";
        public static ulong guildId = 0;
        public static ulong categoryId = 0;
        public static DiscordSocketConfig config = new DiscordSocketConfig();

        public static List<Member> members = LoadMembersFromJson("members.json");
        public static List<Ticket> tickets = new List<Ticket>();
        public static List<Admin> admins = LoadAdminsFromJson("admins.json");
        public static int duration=LoadDurationFromJson("duration.json");
        #endregion

        #region Main
        private static void Main(string[] args)
        {
            var str =File.ReadAllText("config.txt");
            guildId = ulong.Parse(str.Split(',')[0]);
            categoryId = ulong.Parse(str.Split(',')[1]);
            botToken= str.Split(',')[2];
            MainAsync().GetAwaiter().GetResult();
        }
        public static async Task MainAsync()
        {
            try
            {
                _client = new DiscordSocketClient(config);

                _client.Log += _client_Log;
                _client.Ready += _client_Ready;
                _client.SlashCommandExecuted += _client_SlashCommandExecuted;
                _client.ButtonExecuted += _client_ButtonExecuted;
                _client.ModalSubmitted += _client_ModalSubmitted;

                await _client.LoginAsync(TokenType.Bot, botToken);
                await _client.StartAsync();

                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MainAsync: {ex.Message}");
            }
        }

       
        #endregion

        #region Clients
        private static Task _client_Log(LogMessage arg)
        {
            Console.WriteLine($"{arg.Message}");
            return Task.CompletedTask;
        }
        private static async Task _client_Ready()
        {
            try
            {
                List<ChannelType> channelTypes = new List<ChannelType>() {ChannelType.Category};
                SlashCommandBuilder createTicket = new SlashCommandBuilder()
                    .WithName("create-ticket")
                    .WithDescription("Lets create a ticket");
                SlashCommandBuilder clearCooldown = new SlashCommandBuilder()
                    .WithName("clear-cooldown")
                    .WithDescription("Clear the cooldown of any user you want.")
                    .AddOption("username", ApplicationCommandOptionType.Mentionable, "Whos cooldown do you want to clear?", isRequired: true);
                SlashCommandBuilder changeDuration = new SlashCommandBuilder()
                    .WithName("change-duration")
                    .WithDescription("Change the duration of the cooldown.")
                    .AddOption("hours", ApplicationCommandOptionType.Integer, "What is the new duration?", isRequired: true);
                SlashCommandBuilder reviews = new SlashCommandBuilder()
                    .WithName("reviews")
                    .WithDescription("See users ticket experiences.")
                    .AddOption("username", ApplicationCommandOptionType.Mentionable, "Whos reviews do you want to see?");
                SlashCommandBuilder liderBoard = new SlashCommandBuilder()
                    .WithName("leader-board")
                    .WithDescription("See admins leader board.");
                SlashCommandBuilder setCategory = new SlashCommandBuilder()
                   .WithName("set-category")
                   .WithDescription("Set Category.")
                   .AddOption("category",ApplicationCommandOptionType.Channel,"Choose the category you want",channelTypes:channelTypes);


                _ = await _client.CreateGlobalApplicationCommandAsync(createTicket.Build());
                _ = await _client.CreateGlobalApplicationCommandAsync(clearCooldown.Build());
                _ = await _client.CreateGlobalApplicationCommandAsync(changeDuration.Build());
                _ = await _client.CreateGlobalApplicationCommandAsync(reviews.Build());
                _ = await _client.CreateGlobalApplicationCommandAsync(liderBoard.Build());
                _ = await _client.CreateGlobalApplicationCommandAsync(setCategory.Build());



            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in clientReady: {ex}");
            }
        }
        private static async Task _client_SlashCommandExecuted(SocketSlashCommand arg)
        {

            try
            {
                if (arg.Data.Name=="set-category")
                {
                    var category =arg.Data.Options.First().Value as IGuildChannel;
                    categoryId=category.Id;

                }
                if (categoryId == 0)
                {
                    await arg.RespondAsync("Please first set a category using  /set-category command !!!",ephemeral:true);
                    return;
                }
                if ((_client.GetChannel(categoryId) as SocketCategoryChannel).Channels.ToList().Count()>30)
                {
                    await arg.RespondAsync("The category has over 30 channels.Please set a new category using  /set-category command !!! ", ephemeral: true);
                }
                switch (arg.Data.Name)
                { 

                    case "create-ticket":
                        await createTicket(arg);
                        break;
                    case "clear-cooldown":
                         await clearCooldown(arg);
                        break;
                    case "change-duration":
                        await changeDuration(arg);
                        break;
                    case "reviews":
                        await seeReviews(arg);
                        break;
                    case "lider-board":
                        await seeAdminsLeaderBoard(arg);
                        break;          
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error in clientReady: {ex}");
            }
        }
        private static async Task _client_ButtonExecuted(SocketMessageComponent arg)
        {
            try
            {
                switch (arg.Data.CustomId.Split('#')[0])
                {
                    case "submit":
                        await inputInfo(arg, arg.Data.CustomId.Split('#')[1]);
                        break;
                    case "close":
                        await closeTicket(arg, ulong.Parse(arg.Data.CustomId.Split('#')[1].ToString()), arg.Data.CustomId.Split('#')[2]);
                        break;
                    case "up":
                        await upRate(arg, ulong.Parse(arg.Data.CustomId.Split('#')[1].ToString()), arg.Data.CustomId.Split('#')[2].ToString());
                        break;
                    case "down":
                        await downRate(arg, ulong.Parse(arg.Data.CustomId.Split('#')[1].ToString()), arg.Data.CustomId.Split('#')[2].ToString());
                        break;
                    case "ratesubmit":
                        await feedbackModal(arg, ulong.Parse(arg.Data.CustomId.Split('#')[1].ToString()), arg.Data.CustomId.Split('#')[2]);
                        break;
                    case "next":
                        await nextPageReview(arg,int.Parse(arg.Data.CustomId.Split('#')[1]), int.Parse(arg.Data.CustomId.Split('#')[2]), int.Parse(arg.Data.CustomId.Split('#')[3]));
                        break;
                    case "previous":
                        await previousPageReview(arg, int.Parse(arg.Data.CustomId.Split('#')[1]), int.Parse(arg.Data.CustomId.Split('#')[2]), int.Parse(arg.Data.CustomId.Split('#')[3]));
                        break;
                    case "nextlb":
                        await nextPageLiderBoard(arg, int.Parse(arg.Data.CustomId.Split('#')[1]), int.Parse(arg.Data.CustomId.Split('#')[2]), int.Parse(arg.Data.CustomId.Split('#')[3]));
                        break;
                    case "previouslb":
                        await previousPageLiderBoard(arg, int.Parse(arg.Data.CustomId.Split('#')[1]), int.Parse(arg.Data.CustomId.Split('#')[2]), int.Parse(arg.Data.CustomId.Split('#')[3]));
                        break;
                    default:
                        break;
                }
               
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in clientReady: {ex}");
            }
        }
        private async static Task _client_ModalSubmitted(SocketModal arg)
        {
            try
            {
                switch (arg.Data.CustomId.Split('#')[0].ToString())
                {
                    case "info":
                        await createChannel(arg);
                        break;
                    case "feedback":
                        await submitRate(arg, ulong.Parse(arg.Data.CustomId.Split('#')[1]), arg.Data.CustomId.Split('#')[2]);
                        await arg.DeferAsync();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in clientReady: {ex}");
            }
        }
        #endregion

        #region Ticket
        public static async Task seeAdminsLeaderBoard(SocketSlashCommand arg)
        {
            const int MembersPerPage = 10;
            int currentPageIndex = 0;
            int totalPageNumber = 0;
            int totalReviewNumber = 0;

           foreach(Admin admin in admins)
            {
                totalReviewNumber++;
            }
            totalPageNumber = (totalReviewNumber / MembersPerPage) + ((totalReviewNumber % MembersPerPage == 0) ? 0 : 1);


            var sortedAdmins = admins.OrderByDescending(admin => admin.totalStarCount).ToList();

            var leaderBoardEmbed = new EmbedBuilder()
                .WithTitle($"Admins Leader Board")
                .WithAuthor(_client.CurrentUser)
                .WithColor(Color.Magenta)
                .WithCurrentTimestamp();
            var previousButton = new ButtonBuilder()
                 .WithLabel("⬅️ Previous Page")
                 .WithCustomId($"previouslb#{currentPageIndex}#{totalReviewNumber}#{MembersPerPage} #false")
                 .WithStyle(ButtonStyle.Danger);
            var currentPageButton = new ButtonBuilder()
                .WithLabel($"Current Page {currentPageIndex + 1}/{totalPageNumber}")
                .WithCustomId("current#")
                .WithStyle(ButtonStyle.Primary)
                .WithDisabled(true);
            var nextPageButton = new ButtonBuilder()
                .WithLabel(" Next Page ➡️")
                .WithCustomId($"nextlb#{currentPageIndex}#{totalReviewNumber}#{MembersPerPage} #false")
                .WithStyle(ButtonStyle.Success);

            int startIndex = currentPageIndex * MembersPerPage;
            int endIndex = startIndex + MembersPerPage;

            for (int j = startIndex; j < endIndex && j < members.Count; j++)
            {
                    Admin admin = sortedAdmins[j];
               
                    leaderBoardEmbed.Description += ($"{j+1} - <@{admin.userId}>  : {admin.totalStarCount} stars, {admin.ticketIds.Count} tickets\n");
            }

            if (currentPageIndex == 0)
            {
                previousButton.WithDisabled(true);
            }
            if ((currentPageIndex + 1) == (totalPageNumber))
            {
                nextPageButton.WithDisabled(true);
            }
            var builder = new ComponentBuilder().WithButton(previousButton).WithButton(currentPageButton).WithButton(nextPageButton).Build();
            await arg.RespondAsync(components:builder,embed: leaderBoardEmbed.Build(), ephemeral: true);

        }
        public static async Task nextPageLiderBoard(SocketMessageComponent arg, int currentPage, int totalReviewNumber, int MembersPerPage)
        {
            currentPage++;

            await modifySeeAdminsLeaderBoard(arg, currentPage, totalReviewNumber, MembersPerPage);


        }
        public static async Task previousPageLiderBoard(SocketMessageComponent arg, int currentPage, int totalReviewNumber, int MembersPerPage)
        {
            currentPage--;

            await modifySeeAdminsLeaderBoard(arg, currentPage, totalReviewNumber, MembersPerPage);

        }
        public static async Task modifySeeAdminsLeaderBoard(SocketMessageComponent arg, int currentPageIndex, int totalReviewNumber, int MembersPerPage) {
            await arg.DeferAsync();
            int totalPageNumber = totalReviewNumber / MembersPerPage;

            var sortedAdmins = admins.OrderByDescending(admin => admin.totalStarCount).ToList();

            var leaderBoardEmbed = new EmbedBuilder()
                .WithTitle($"Admins Leader Board")
                .WithAuthor(_client.CurrentUser)
                .WithColor(Color.Magenta)
                .WithCurrentTimestamp();
            var previousButton = new ButtonBuilder()
                 .WithLabel("⬅️ Previous Page")
                 .WithCustomId($"previouslb#{currentPageIndex}#{totalReviewNumber}#{MembersPerPage} #false")
                 .WithStyle(ButtonStyle.Danger);
            var currentPageButton = new ButtonBuilder()
                .WithLabel($"Current Page {currentPageIndex + 1}/{totalPageNumber}")
                .WithCustomId("current#")
                .WithStyle(ButtonStyle.Primary)
                .WithDisabled(true);
            var nextPageButton = new ButtonBuilder()
                .WithLabel(" Next Page ➡️")
                .WithCustomId($"nextlb#{currentPageIndex}#{totalReviewNumber}#{MembersPerPage} #false")
                .WithStyle(ButtonStyle.Success);

            int startIndex = currentPageIndex * MembersPerPage;
            int endIndex = startIndex + MembersPerPage;

            for (int j = startIndex; j < endIndex && j < members.Count; j++)
            {
                Admin admin = sortedAdmins[j];

                leaderBoardEmbed.Description += ($"{j + 1} - <@{admin.userId}>  : {admin.totalStarCount} stars, {admin.ticketIds.Count} tickets\n");
            }

            if (currentPageIndex == 0)
            {
                previousButton.WithDisabled(true);
            }
            if ((currentPageIndex + 1) == (totalPageNumber))
            {
                nextPageButton.WithDisabled(true);
            }
            var builder = new ComponentBuilder().WithButton(previousButton).WithButton(currentPageButton).WithButton(nextPageButton).Build();

            await arg.ModifyOriginalResponseAsync(res =>
            {
                res.Embed = leaderBoardEmbed.Build();
                res.Components = builder;
            });

        }
        public static async Task closeTicket(SocketMessageComponent arg, ulong id, string userId)
        {
            Console.WriteLine(id);
            Admin admin = admins.FirstOrDefault(u => u.userId == arg.User.Id.ToString());
            if (admin == null)
            {
                admin = new Admin
                {
                    userId = arg.User.Id.ToString(),
                };
                admins.Add(admin);
            }
            Member member = members.Find(c => c.userId == userId);

            Ticket ticket = member.tickets.Find(c => c.id == id);

            if (arg.User.Id.ToString() == member.userId)
            {
                await arg.RespondAsync("Only staff members can interact with this ticket.", ephemeral: true);
                return;
            }
            Console.WriteLine(ticket.id);
            admin.ticketIds.Add(ticket.id);
            ticket.adminsUserId = admin.userId;
         
            var ratingEmbed = new EmbedBuilder()
                 .WithTitle($"Rate your experience and write feedback")
                 .WithAuthor(_client.CurrentUser)
                 .WithColor(Color.Magenta)
                 .WithCurrentTimestamp()
                 .Build();

            var rateDownButton = new ButtonBuilder()
                .WithLabel("⬇️ Rate Down")
                .WithCustomId("down#" + id + "#" + userId)
                .WithStyle(ButtonStyle.Danger);

            var showStars = new ButtonBuilder()
               .WithLabel("⭐")
               .WithCustomId("stars#")
               .WithStyle(ButtonStyle.Primary)
               .WithDisabled(true);

            var rateUpButton = new ButtonBuilder()
               .WithLabel(" Rate Up ⬆️")
               .WithCustomId("up#" + id + "#" + userId)
               .WithStyle(ButtonStyle.Success);

            var rateSubmitButton = new ButtonBuilder()
             .WithLabel("Submit")
             .WithCustomId("ratesubmit#" + id + "#" + userId)
             .WithStyle(ButtonStyle.Primary);


            var builder = new ComponentBuilder().WithButton(rateDownButton).WithButton(showStars).WithButton(rateUpButton).WithButton(rateSubmitButton, row: 2).Build();
            await arg.RespondAsync($"<@{member.userId}> you have to rate the admin to end this ticket! ", components: builder, embed: ratingEmbed);
        }
        public static async Task upRate(SocketMessageComponent arg,ulong id,string userId)
        {       
            Member member = members.Find(c => c.userId == userId);

            if (arg.User.Id.ToString()!=member.userId)
            {
                await arg.RespondAsync("Only members can interact with this ticket.", ephemeral: true);
                return;
            }

            member.tickets.Find(c=>c.id==id).rate++;
            Console.WriteLine(member.tickets.Find(c => c.id == id).rate);
            await modifyRatingEmbedAndButtons(arg, member.tickets.Find(c => c.id == id).rate, id,userId);

        }
        public static async Task downRate(SocketMessageComponent arg, ulong id,string userId)
        {
            Member member = members.Find(c => c.userId == userId);

            if (arg.User.Id.ToString() != member.userId)
            {
                await arg.RespondAsync("Only members can interact with this ticket.", ephemeral: true);
                return;
            }

            member.tickets.Find(c => c.id == id).rate--;
            Console.WriteLine(member.tickets.Find(c => c.id == id).rate);
            await modifyRatingEmbedAndButtons(arg, member.tickets.Find(c => c.id == id).rate,id,userId);
        }
        public static async Task modifyRatingEmbedAndButtons(SocketMessageComponent arg,int starCount,ulong id,string userId)
        {
            try
            {
                string[] stars = { "⭐", "⭐⭐", "⭐⭐⭐", "⭐⭐⭐⭐", "⭐⭐⭐⭐⭐" };

                await arg.DeferAsync();

                var ratingEmbed = new EmbedBuilder()
                    .WithTitle($"Rate your experience and write a feedback")
                    .WithAuthor(_client.CurrentUser)
                    .WithColor(Color.Magenta)
                    .WithCurrentTimestamp()
                    ;
                var rateDownButton = new ButtonBuilder()
                    .WithLabel("⬇️ Rate Down")
                    .WithCustomId("down#" + id + "#" + userId)
                    .WithStyle(ButtonStyle.Danger);
                var showStars = new ButtonBuilder()
                    .WithLabel($"{stars[starCount - 1]}")
                    .WithCustomId("stars#")
                    .WithStyle(ButtonStyle.Primary)
                    .WithDisabled(true);
                var rateUpButton = new ButtonBuilder()
                    .WithLabel(" Rate Up ⬆️")
                    .WithCustomId("up#" + id + "#" + userId)
                    .WithStyle(ButtonStyle.Success);
                var rateSubmitButton = new ButtonBuilder()
                    .WithLabel("Submit")
                    .WithCustomId("ratesubmit#" + id + "#" + userId)
                    .WithStyle(ButtonStyle.Primary);

                if (starCount==5)
                {
                    rateUpButton.WithDisabled(true);
                }
                if (starCount==1)
                {
                    rateDownButton.WithDisabled(true);
                }

                var builder = new ComponentBuilder().WithButton(rateDownButton).WithButton(showStars).WithButton(rateUpButton).WithButton(rateSubmitButton, row: 2);


                //await arg.RespondAsync(embed: ratingEmbed.Build(), components: builder.Build(),ephemeral:true);
                await arg.ModifyOriginalResponseAsync(res =>
                {
                    res.Embed = ratingEmbed.Build();
                    res.Components = builder.Build();
                });
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error in modify: {ex}");
            }
           
        }
        public static async Task feedbackModal(SocketMessageComponent arg,ulong id,string userId) {
            ModalBuilder feedback = new ModalBuilder()
                         .WithTitle("Input a feedback")
                         .WithCustomId("feedback#" + id + "#" + userId)
                         .AddTextInput("Input a feedback", "feedbackmsg", placeholder: "Input a feedback")
                         ;

            await arg.RespondWithModalAsync(modal: feedback.Build());
        }
        public static async Task submitRate(SocketModal arg, ulong id,string userId)
        {
            string feedback = arg.Data.Components.FirstOrDefault(x => x.CustomId == "feedbackmsg").Value;
            Member member = members.Find(c => c.userId == userId);

            if (arg.User.Id.ToString() != member.userId)
            {
                await arg.RespondAsync("Only members can interact with this ticket.", ephemeral: true);
                return;
            }

            Admin admin = admins.Find(c=>c.userId==userId);
            var ticket= member.tickets.Find(c => c.id == id);
            admin.totalStarCount += ticket.rate;
            ticket.experience=feedback;        
            Member.SaveAllToJson(members,"members.json");
            Admin.SaveAllToJson(admins, "admins.json");
            var channel = arg.Channel as SocketGuildChannel;

            if (channel != null)
            {
                await channel.DeleteAsync();
            }
        }
        public static async Task inputInfo(SocketMessageComponent arg,string userId)
        {
            try
            {
                Member member = members.Find(c=>c.userId== userId);   
                ModalBuilder info = new ModalBuilder()
                         .WithTitle("Input an aim")
                         .WithCustomId("info#")
                         .AddTextInput("Input an aim", "aim", placeholder: "Input an aim")
                         .AddTextInput("Input a topic", "topic", placeholder: "Input a topic")
                         .AddTextInput("Input a question", "question", placeholder: "Input a question");

                await arg.RespondWithModalAsync(modal: info.Build());

            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error in clientReady: {ex}");
                ;
            }
        }
        public static async Task createTicket(SocketSlashCommand arg)
        {
            Member member = members.FirstOrDefault(u => u.userId == arg.User.Id.ToString());
            if (member == null)
            {
                member = new Member
                {
                    userId = arg.User.Id.ToString(),
                    userName=arg.User.Username                    
                };

                members.Add(member);
            }
            Console.WriteLine(duration);

            if (DateTime.Now>member.cooldown)
            {
                EmbedBuilder ticketEmbed = new EmbedBuilder()
              .WithTitle("Create A Ticket")
              .WithDescription("Click the button for create a ticket.")
              .WithAuthor(_client.CurrentUser)
              .WithColor(Color.Magenta)
              .WithCurrentTimestamp()
              .WithImageUrl("https://avatars.githubusercontent.com/u/80770468?s=280&v=4");

                ButtonBuilder submitButton = new ButtonBuilder()
                   .WithLabel($"submit your clip!")
                   .WithCustomId($"submit#{member.userId}")
                   .WithStyle(ButtonStyle.Primary);

                ComponentBuilder builder = new ComponentBuilder()
                    .WithButton(submitButton);

                await arg.RespondAsync(components: builder.Build(), embed: ticketEmbed.Build(), ephemeral: true);
            }
            else
            {
                TimeSpan timeRemaining = member.cooldown - DateTime.Now;
                 var warningEmbed = new EmbedBuilder()
                .WithTitle("Unfortunately,you can't create a ticket at now.")
                .WithDescription($"You can create a new ticket after {timeRemaining.Days} days later.")
                .WithAuthor(_client.CurrentUser)
                .WithColor(Color.Magenta)
                .WithCurrentTimestamp()
                .Build()
                ;
                await arg.RespondAsync(embed: warningEmbed, ephemeral: true);

            }


        }     
        public static async Task createChannel(SocketModal arg)
        {
            try
            {
                await arg.DeferAsync();
                Member member = members.Find(c => c.userId == arg.User.Id.ToString());
                member.cooldown = DateTime.Now + TimeSpan.FromHours(duration);
                await Console.Out.WriteLineAsync(member.cooldown.ToString());
                Ticket myTicket = new Ticket();

                SocketGuild guild = _client.GetGuild(guildId);

                if (guild != null)
                {
                    RestTextChannel channel = await guild.CreateTextChannelAsync($"ticket-{arg.User.Id}", c =>
                    {
                        c.CategoryId = categoryId;

                        SocketRole everyoneRole = guild.EveryoneRole;
                        SocketRole staffRole = guild.Roles.FirstOrDefault(x => x.Name.Contains("Staff"));

                        if (everyoneRole != null && staffRole != null)
                        {
                            c.PermissionOverwrites = new List<Overwrite>()
                    {
                        new Overwrite(everyoneRole.Id, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Deny)),
                        new Overwrite(staffRole.Id, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Allow)),
                        new Overwrite(arg.User.Id, PermissionTarget.User, new OverwritePermissions(viewChannel: PermValue.Allow))
                    };
                        }
                        else
                        {
                            Console.WriteLine("Error: EveryoneRole or StaffRole is null.");
                        }
                    });                

                    string aim = arg.Data.Components.FirstOrDefault(x => x.CustomId == "aim").Value;
                    string topic = arg.Data.Components.FirstOrDefault(x => x.CustomId == "topic").Value;
                    string question = arg.Data.Components.FirstOrDefault(x => x.CustomId == "question").Value;

                    myTicket.aim= aim;
                    myTicket.topic= topic;
                    myTicket.question= question;

                    var createdInfoembed = new EmbedBuilder()
                        .WithTitle("Ticket Created")
                        .WithDescription($"A new ticket has been created by {arg.User.Mention}.\nAim : {aim}\nTopic : {topic}\nQuestion : {question}")
                        .WithFooter("Thank you for creating a ticket.Our staff team will asist you asap.\nClose button is only available to administrators!")
                        .WithColor(Color.Blue)
                        .WithCurrentTimestamp()
                        .WithAuthor(_client.CurrentUser)
                        .Build();
                    
                    var closeButton = new ButtonBuilder()
                        .WithLabel($"❌ End Review")
                        .WithCustomId($"close#{myTicket.id}#{member.userId}")
                        .WithStyle(ButtonStyle.Secondary) 
                        ;
                    var builder = new ComponentBuilder().WithButton(closeButton).Build();

                    await channel.SendMessageAsync(embed: createdInfoembed,components:builder);

                    var embed=new EmbedBuilder()
                         .WithTitle("Your Ticket Has Been Created")
                        .WithDescription($"Thank you for creating a ticket")
                        .WithColor(Color.Blue)
                        .WithCurrentTimestamp()
                        .WithAuthor(_client.CurrentUser)
                        .Build();
                    var emptyBuilder=new ComponentBuilder().Build();

                    await arg.ModifyOriginalResponseAsync(res =>
                    {
                        res.Embed = embed;
                        res.Components = emptyBuilder;
                    });


                    tickets.Add(myTicket);
                    member.tickets.Add(myTicket);

                    Member.SaveAllToJson(members, "members.json");
                }
                else
                {
                    Console.WriteLine("Error: Guild is null.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateChannel: {ex}");
            }
        }
        public static async Task clearCooldown(SocketSlashCommand arg) {
            var userName = arg.Data.Options.FirstOrDefault(option => option.Name == "username").Value.ToString();
            Console.WriteLine(userName);
            Member member = members.Find(c=>c.userName==userName);
            if (member != null)
            {
                member.cooldown = DateTime.Now;
            }
            else
            {
                Console.WriteLine("member null");
            }

            var clearCooldownEmbed = new EmbedBuilder()
          .WithTitle($" {userName}'s Cooldown has been cleared.")
          .WithAuthor(_client.CurrentUser)
          .WithColor(Color.Magenta)
          .WithCurrentTimestamp()
          .Build()
          ;

            await arg.RespondAsync(embed: clearCooldownEmbed,ephemeral:true);
        }
        public static async Task changeDuration(SocketSlashCommand arg)
        {
            var newDuration = int.Parse(arg.Data.Options.FirstOrDefault(option => option.Name == "hours").Value.ToString());
            duration = newDuration;
            SaveDurationToJson(duration, "duration.json");

            var changeDurationEmbed = new EmbedBuilder()
                 .WithTitle($"duraiton has been changed as {newDuration} hours.")
                 .WithAuthor(_client.CurrentUser)
                 .WithColor(Color.Magenta)
                 .WithCurrentTimestamp()
                 .Build()
                 ;


            await arg.RespondAsync(embed: changeDurationEmbed, ephemeral: true);

        }
        public static async Task seeReviews(SocketSlashCommand arg)
        {
            const int MembersPerPage = 1;
            int currentPageIndex = 0;
            int totalPageNumber=0;
            int totalReviewNumber = 0;

            if (arg.Data.Options.Any(o => o.Name == "username"))
            {
                string targetUsername = arg.Data.Options.First(o => o.Name == "username").Value.ToString();

                EmbedBuilder reviewsListEmbed = new EmbedBuilder()
                    .WithTitle($"Reviews for {targetUsername}")
                    .WithAuthor(_client.CurrentUser)
                    .WithColor(Color.Magenta)
                    .WithCurrentTimestamp();

                foreach (Member member in members)
                {
                    // Check if the member's username matches the targetUsername
                    if (member.userName == targetUsername)
                    {
                        foreach (Ticket ticket in member.tickets)
                        {
                            if (ticket.experience != "")
                            {
                                reviewsListEmbed.Description += ($"By: <@{member.userId}> To: <@{ticket.adminsUserId}>\nRate: {ticket.rate}\nFeedback Message: {ticket.experience}\nAim: {ticket.aim}\nTopic: {ticket.topic}\nQuestion: {ticket.question}\n\n");
                                totalReviewNumber++;
                            }
                        }
                    }
                }
                Console.WriteLine(totalReviewNumber);
                totalPageNumber = totalReviewNumber / MembersPerPage;
                Console.WriteLine(totalPageNumber);
                var previousButton = new ButtonBuilder()
                  .WithLabel("⬅️ Previous Page")
                  .WithCustomId($"previous#{currentPageIndex}#{totalReviewNumber}#{MembersPerPage}")
                  .WithStyle(ButtonStyle.Danger);
                var currentPageButton = new ButtonBuilder()
                    .WithLabel($"Current Page {currentPageIndex + 1}/{totalReviewNumber} ")
                    .WithCustomId($"current#")
                    .WithStyle(ButtonStyle.Primary)
                    .WithDisabled(true);
                var nextPageButton = new ButtonBuilder()
                    .WithLabel(" Next Page ➡️")
                    .WithCustomId($"next#{currentPageIndex}#{totalReviewNumber}#{MembersPerPage}")
                    .WithStyle(ButtonStyle.Success);
                var builder = new ComponentBuilder().WithButton(previousButton).WithButton(currentPageButton).WithButton(nextPageButton).Build();

                



                if (currentPageIndex == 0)
                {
                    previousButton.WithDisabled(true);
                }
                if (currentPageIndex ==(totalPageNumber-1))
                {
                    nextPageButton.WithDisabled(true);
                }

                await arg.RespondAsync(components:builder,embed: reviewsListEmbed.Build(), ephemeral: true);
            }
            else
            {
                foreach (Member member in members)
                {
                    foreach (Ticket ticket in member.tickets)
                    {
                        if (ticket.experience != "")
                        {
                            totalReviewNumber++;
                        }
                    }
                }
                totalPageNumber = (totalReviewNumber / MembersPerPage) + ((totalReviewNumber % MembersPerPage == 0) ? 0 : 1);

                EmbedBuilder reviewsListEmbed = new EmbedBuilder()
                    .WithTitle("All Reviews")
                    .WithAuthor(_client.CurrentUser)
                    .WithColor(Color.Magenta)
                    .WithCurrentTimestamp();
                var previousButton = new ButtonBuilder()
                  .WithLabel("⬅️ Previous Page")
                  .WithCustomId($"previous#{currentPageIndex}#{totalReviewNumber}#{MembersPerPage}#true")
                  .WithStyle(ButtonStyle.Danger);
                var currentPageButton = new ButtonBuilder()
                    .WithLabel($"Current Page {currentPageIndex+1}/{totalPageNumber}")
                    .WithCustomId("current#")
                    .WithStyle(ButtonStyle.Primary)
                    .WithDisabled(true);
                var nextPageButton = new ButtonBuilder()
                    .WithLabel(" Next Page ➡️")
                    .WithCustomId($"next#{currentPageIndex}#{totalReviewNumber}#{MembersPerPage}#true")
                    .WithStyle(ButtonStyle.Success);

                int startIndex = currentPageIndex * MembersPerPage;
                int endIndex = startIndex + MembersPerPage;

                for (int i = startIndex; i < endIndex && i < members.Count; i++)
                {
                    Member member = members[i];
                    foreach (Ticket ticket in member.tickets)
                    {
                        if (ticket.experience != "")
                        {
                            reviewsListEmbed.Description+=($"By: <@{member.userId}> To: <@{ticket.adminsUserId}>\nRate: {ticket.rate}\nFeedback Message: {ticket.experience}\nAim: {ticket.aim}\nTopic: {ticket.topic}\nQuestion: {ticket.question}\n\n");
                        }
                    }
                }
                Console.WriteLine(totalPageNumber);

                if (currentPageIndex == 0)
                {
                    previousButton.WithDisabled(true);
                }
                if ((currentPageIndex+1) == (totalPageNumber))
                {
                    nextPageButton.WithDisabled(true);
                }
                var builder = new ComponentBuilder().WithButton(previousButton).WithButton(currentPageButton).WithButton(nextPageButton).Build();

                await arg.RespondAsync(components: builder, embed: reviewsListEmbed.Build(), ephemeral: true);
            }
        }
        public static async Task modifySeeReviews(SocketMessageComponent arg,int currentPageIndex,int totalReviewNumber,int MembersPerPage)
        {

            await arg.DeferAsync();

            int totalPageNumber = totalReviewNumber / MembersPerPage;

            EmbedBuilder reviewsListEmbed = new EmbedBuilder()
                    .WithTitle("All Reviews")
                    .WithAuthor(_client.CurrentUser)
                    .WithColor(Color.Magenta)
                    .WithCurrentTimestamp();
                var previousButton = new ButtonBuilder()
                  .WithLabel("⬅️ Previous Page")
                  .WithCustomId($"previous#{currentPageIndex}#{totalReviewNumber}#{MembersPerPage} #true")
                  .WithStyle(ButtonStyle.Danger);
                var currentPageButton = new ButtonBuilder()
                    .WithLabel($"Current Page {currentPageIndex+1}/{totalPageNumber}")
                    .WithCustomId("current#")
                    .WithStyle(ButtonStyle.Primary)
                    .WithDisabled(true);
                var nextPageButton = new ButtonBuilder()
                    .WithLabel(" Next Page ➡️")
                    .WithCustomId($"next#{currentPageIndex}#{totalReviewNumber}#{MembersPerPage} #true")
                    .WithStyle(ButtonStyle.Success);

                int startIndex = currentPageIndex * MembersPerPage;
                int endIndex = startIndex + MembersPerPage;

                for (int i = startIndex; i < endIndex && i < members.Count; i++)
                {
                    Member member = members[i];
                    foreach (Ticket ticket in member.tickets)
                    {
                        if (ticket.experience != "")
                        {
                        reviewsListEmbed.Description += ($"By: <@{member.userId}> To: <@{ticket.adminsUserId}>\nRate: {ticket.rate}\nFeedback Message: {ticket.experience}\nAim: {ticket.aim}\nTopic: {ticket.topic}\nQuestion: {ticket.question}\n\n");
                    }
                }
                }

            if (currentPageIndex == 0)
            {
                previousButton.WithDisabled(true);
            }
            if ((currentPageIndex + 1) == (totalPageNumber))
            {
                Console.WriteLine( "aaaa");
                nextPageButton.WithDisabled(true);
            }
            var builder = new ComponentBuilder().WithButton(previousButton).WithButton(currentPageButton).WithButton(nextPageButton).Build();


            await arg.ModifyOriginalResponseAsync(res =>
                {
                    res.Embed = reviewsListEmbed.Build();
                    res.Components = builder;
                });
            
        }
        public static async Task nextPageReview(SocketMessageComponent arg,int currentPage,int totalReviewNumber, int MembersPerPage)
        {
            currentPage++;
            
             await modifySeeReviews(arg, currentPage, totalReviewNumber, MembersPerPage);

            
        }
        public static async Task previousPageReview(SocketMessageComponent arg, int currentPage, int totalReviewNumber, int MembersPerPage)
        {
            currentPage--;
           
            await modifySeeReviews(arg, currentPage, totalReviewNumber, MembersPerPage);
            
        }
        private static List<Member> LoadMembersFromJson(string filePath)
        {
            if (File.Exists(filePath))
            {

                string json = File.ReadAllText(filePath);
                return json.Length != 0 ? JsonConvert.DeserializeObject<List<Member>>(json) : new List<Member>();
            }
            else
            {
                Console.WriteLine("dosya bulunamadı");
            }
            return new List<Member>();
        }
        private static List<Admin> LoadAdminsFromJson(string filePath)
        {
            if (File.Exists(filePath))
            {

                string json = File.ReadAllText(filePath);
                return json.Length != 0 ? JsonConvert.DeserializeObject<List<Admin>>(json) : new List<Admin>();
            }
            else
            {
                Console.WriteLine("dosya bulunamadı");
            }
            return new List<Admin>();
        }
        public static void SaveDurationToJson(int duration, string filePath)
        {
            try
            {
                string json = JsonConvert.SerializeObject(duration, Formatting.Indented);
                File.WriteAllText(filePath, json);
                Console.WriteLine($"duartion saved to {filePath} successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving duration to {filePath}: {ex.Message}");
            }
        }
        private static int LoadDurationFromJson(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    return !string.IsNullOrEmpty(json) ? JsonConvert.DeserializeObject<int>(json) : 168;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Dosya okuma hatası: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Dosya bulunamadı");
            }

            return 168;
        }

        #endregion
    }
}
