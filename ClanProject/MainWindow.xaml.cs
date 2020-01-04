using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClanProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            random = new Random();
            dynastyMembers = new List<Member>();
        }

        int numberOfFounders;
        int startingYear, currentYear;
        List<Member> dynastyMembers;
        int counter;
        Random random;

        private void Slide_NumberOfFounders_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = (Slider)sender;
            int currentValue = (int) slider.Value;
            lbl_NumberOfFounders.Content = currentValue;
        }

        private void Slide_YearLimit_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = (Slider)sender;
            int currentValue = (int)slider.Value;
            lbl_YearLimit.Content = currentValue;
        }

        private void Btn_Start_Click(object sender, RoutedEventArgs e)
        {
            numberOfFounders = (int) slide_NumberOfFounders.Value;
            startingYear = DateTime.Now.Year - (int)slide_YearLimit.Value;

            do { BuildDynasty(); } 
            while (!BalancedDynasty());
            PrintDynasty();

            MessageBox.Show("Done!");
        }

        private void BuildDynasty()
        {
            dynastyMembers.Clear();
            currentYear = startingYear;
            counter = 1;

            //make the founders
            for (int i = 0; i < numberOfFounders; i++)
            {
                dynastyMembers.Add(Member.CreateNewMember(random, startingYear, ref counter, (Member.FamilyGroup)i + 1));
            }

            //for each year
            while(currentYear <= DateTime.Now.Year && (BalancedDynasty() || currentYear < startingYear + 100))
            {
                
                currentYear++;
                //for each member
                for (int b = 0; b < dynastyMembers.Count; b++)
                {
                    var bride = dynastyMembers[b];

                    //if they are alive
                    if(bride.YearOfDeath == null)
                    {
                        //determine death
                        if (bride.WillDie(random, currentYear))
                        {
                            bride.YearOfDeath = mDate.ReturnRandomBirthday(random, currentYear);

                            //determine the manner of death
                            bride.MannerOfDeath = bride.GetMannerOfDeath(random, currentYear);

                            //Fraternal twins always die at the same time
                            if(bride.Twins == Member.TwinGroup.Fraternal)
                            {
                                //find the character's twin & assign the same DOD and manner of death
                                Member Twin = bride.Twin;
                                Twin.YearOfBirth = bride.YearOfBirth;
                                Twin.MannerOfDeath = bride.MannerOfDeath;
                            }
                        }
                        else if (bride.Gender == Member.GenderGroup.Female)
                        {
                        //determine marriage
                            if (bride.WillGetMarried(random, currentYear))
                            {
                                List<Member> prospectiveGrooms = new List<Member>();
                                foreach (Member groom in dynastyMembers)
                                {
                                    if (groom.Gender == Member.GenderGroup.Male)
                                    {
                                        //check for compatibility
                                        if (groom.YearOfDeath == null && bride.IsCompatible(bride, groom, currentYear))
                                        {
                                            prospectiveGrooms.Add(groom);
                                        }
                                    }
                                }

                                if(prospectiveGrooms.Count < 5)
                                {
                                    //if there aren't enough options, make some others
                                    for (int i = 0; i < 5; i++)
                                    {
                                        prospectiveGrooms.Add(Member.CreateNewMember(random, bride.YearOfBirth.mYear, ref counter,
                                            Member.FamilyGroup.Outsider, Member.GenderGroup.Male));
                                    }
                                }

                                //choose a random member from the prospective grooms
                                Member chosenGroom = prospectiveGrooms[random.Next(prospectiveGrooms.Count)];

                                //assign spouses
                                bride.Spouse = chosenGroom;
                                chosenGroom.Spouse = bride;

                                //update the bride's name
                                bride.lastName = chosenGroom.lastName + " né " + bride.lastName;

                                //if the groom was created, add him to the dynasty list
                                if(!dynastyMembers.Contains(chosenGroom))
                                {
                                    dynastyMembers.Add(chosenGroom);
                                }
                            }
                            //determine divorce
                            else if (bride.WillGetDivorced(random, currentYear) && bride.Spouse != null)
                            {
                                //dissolve the marriage and make both parties eligible to marry again
                                Member spouse = bride.Spouse;
                                bride.Spouse = null;
                                spouse.Spouse = null;
                            }
                            //determine birth
                            if (bride.WillGiveBirth(random, currentYear) && bride.Spouse != null)
                            {
                                int numberOfChildren = 1;
                                List<Member> children = new List<Member>();

                                Member.TwinGroup twin = Member.GetTwinGroup(random);

                                if (twin != Member.TwinGroup.Single) numberOfChildren = 2;

                                for (int i = 0; i < numberOfChildren; i++)
                                {
                                    children.Add(Member.CreateNewMember(random, currentYear, ref counter, bride, bride.Spouse, twin));
                                    if (i > 0) // if this is a twin, make sure their birthday matches the first member of the children list
                                    {
                                        children[i].YearOfBirth = children[0].YearOfBirth;
                                        children[i].Twin = children[0];
                                        children[0].Twin = children[i];
                                    }
                                }

                                bride.Children.AddRange(children);
                                dynastyMembers.AddRange(children);

                            }
                        }

                    }
                }
            }
        }

        private void PrintDynasty()
        {
            //C:\Users\crank\OneDrive\Documents\
            //C:\Users\atat\Documents\
            string fileName = @"C:\Users\crank\OneDrive\Documents\" + string.Format("{0}_{1}_{2}_{3}_Results.csv", DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute);
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                //header
                sw.WriteLine("ID, Name, Surname, Gender, DOB, DOD, MannerOfDeath, Mother, Father, Family");

                //data
                foreach(Member m in dynastyMembers)
                {
                    string DOB, DOD, mother, father;

                    if (m.YearOfBirth != null)
                    {
                        DOB = m.YearOfBirth.ToString();
                    }
                    else DOB = "NULL";

                    if (m.YearOfDeath != null)
                    {
                        DOD = m.YearOfDeath.ToString();
                    }
                    else DOD = "NULL";

                    if (m.Mother != null)
                    {
                        mother = m.Mother.id.ToString();
                    }
                    else mother = "NULL";
                    if (m.Father != null)
                    {
                        father = m.Father.id.ToString();
                    }
                    else father = "NULL";

                    sw.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}", m.id.ToString(), m.firstName, m.lastName, m.Gender, DOB, DOD, m.MannerOfDeath, 
                        mother, father, m.Family));
                }
            }
        }

        private bool BalancedDynasty()
        {
            bool equalLongevity = true, equalNumbers = true;

            //make sure all families have a reasonably close range of members
            int[] families = new int[numberOfFounders + 1];
            for (int i = 0; i < families.Length; i++) families[i] = 0;

            bool[] livingMembers = new bool[numberOfFounders + 1];
            for (int i = 0; i < families.Length; i++) livingMembers[i] = false;

            foreach (Member m in dynastyMembers)
            {
                families[(int)m.Family]++;

                if (m.YearOfDeath == null)
                    livingMembers[(int)m.Family] = true;
            }

            int smallestFamily = 999999999, largestFamily = 0;
            for(int j = 1; j < families.Length; j++) // start at 1 to exclude all the outsiders brought in for marriage purposes
            {
                if (families[j] < smallestFamily) smallestFamily = families[j];
                if (families[j] > largestFamily) largestFamily = families[j];
            }

            if (largestFamily - smallestFamily > smallestFamily) equalNumbers = false;

            //make sure that all families have surviving members
            for(int k = 1; k < livingMembers.Length; k++)
            {
                if (livingMembers[k] == false) equalLongevity = false;
            }

            if (equalLongevity && equalNumbers)
                return true;
            else return false;
        }
    }

    public class Member
    {
        public override string ToString()
        {
            return string.Format("{0} {1} of House {2}", firstName, lastName, Family);
        }
        public Member()
        {
            Children = new List<Member>();
            MannerOfDeath = MannerOfDeathGroup.Alive;
        }
        public int id;
        public int generation;
        public mDate YearOfBirth, YearOfDeath;
        public FamilyGroup Family;
        public TwinGroup Twins;
        public GenderGroup Gender;
        public MannerOfDeathGroup MannerOfDeath;
        public Member Mother, Father, Spouse, Twin;
        public List<Member> Children;
        public string firstName, lastName;

        public enum FamilyGroup { Outsider, Wolf, Bear, Lion, Tiger, Eagle, Owl, Dragon, Kraken, Stag, Fox, };
        public enum TwinGroup { Fraternal, Identical, Single, }
        public enum GenderGroup { Male, Female, }
        public enum MannerOfDeathGroup { Alive, NaturalCauses, Accident, Violence, Suicide, }

        //for use with founders
        public static Member CreateNewMember(Random random, int in_birthYear, ref int memberCounter, FamilyGroup in_family, 
            GenderGroup in_gender = GenderGroup.Female)
        {
            Member m = new Member() { id = memberCounter, Family = in_family, YearOfBirth = mDate.ReturnRandomBirthday(random, in_birthYear),
                Twins = TwinGroup.Single, Gender = in_gender, generation = 0 };
            m.firstName = GetFirstName(random, m.Gender);
            m.lastName = GetLastName(random);

            memberCounter++;

            return m;
        }
        //for use with 2 parents
        public static Member CreateNewMember(Random random, int in_birthYear, ref int memberCounter, Member mother, Member father, TwinGroup in_twinGroup )
        {
            Member m = new Member()
            {
                id = memberCounter,
                Family = mother.Family,
                Mother = mother,
                Father = father,
                Gender = GetGender(random),
                Twins = in_twinGroup,
                generation = mother.generation + 1,
                YearOfBirth = mDate.ReturnRandomBirthday(random, in_birthYear),
            };
            m.firstName = GetFirstName(random, m.Gender);
            m.lastName = father.lastName;

            memberCounter++;

            return m;
        }

        public bool WillDie(Random random,  int in_currentYear)
        {
            int age = GetAge(in_currentYear);
            int odds;

            if (age < 2) odds = 100;
            else if (age < 16) odds = 21;
            else if (age < 26) odds = 60;
            else if (age < 41) odds = 86;
            else if (age < 66) odds = 200;
            else if (age < 96) odds = 300;
            else if (age < 121) odds = 400;
            else odds = 99999;

            if(generation < 2 && age < 45) odds = 0; // trying to give the founders a better chance at producing a dynasty
        

            if (random.Next(10000) < odds) return true;
            else return false;
        }
        public bool WillGetMarried(Random random, int in_currentYear)
        {
            int age = GetAge(in_currentYear);
            int odds;

            if (age < 15) odds = 0;
            else if (age < 25) odds = 15;
            else if (age < 45) odds = 25;
            else if (age < 61) odds = 3;
            else odds = 99999;

            if (Gender == GenderGroup.Male) return false;
            else if (Twins == TwinGroup.Fraternal) return false;
            else if (random.Next(1000) < odds) return true;
            else return false;
        }
        public bool WillGetDivorced(Random random, int in_currentYear)
        {
            int age = GetAge(in_currentYear);

            if(age < 65) // if they're 65 or over, they're stuck with their spouse until the end
            {
                if (random.Next(10000) == 25)
                    return true;
            }
            return false;
        }
        public bool WillGiveBirth(Random random, int in_currentYear)
        {
            int age = GetAge(in_currentYear);

            //women
            if (Gender == GenderGroup.Female)
            {
                //between 15 & 40
                if(age >= 15 && age <= 40)
                {
                    //married
                    if(Spouse != null)
                    {
                            bool RecentBirth = false;
                        //not given birth in the past year
                        if (Children.Count > 0)
                        {
                            foreach (Member m in Children)
                            {
                                if (m.YearOfBirth.mYear == in_currentYear - 1)
                                {
                                    RecentBirth = true;
                                }
                            }

                        }

                        if (!RecentBirth)
                        {
                            int odds;

                            if (age < 15) odds = 0;
                            else if (age < 25) odds = 200;
                            else if (age < 35) odds = 600;
                            else if (age < 45) odds = 100;
                            else odds = 99999;

                            if (generation < 2) odds *= 3; // give the first generation more of a chance to found the dynasty

                            if (random.Next(1000) < odds) return true;
                        }
                    }
                }
            }
            return false;
        }
        public bool IsCompatible(Member bride, Member groom, int in_currentYear)
        {
            List<Member> bridesRelatives = new List<Member>(), groomsRelatives = new List<Member>();
            //two people cannot share parents or grandparents
            if(bride.Mother != null)
            {
                //pull parents
                bridesRelatives.Add(bride.Mother);
                bridesRelatives.Add(bride.Father);

                //pull grandparents
                if(bride.Mother.Mother != null)
                {
                    bridesRelatives.Add(bride.Mother.Mother);
                    bridesRelatives.Add(bride.Mother.Father);
                }
                if(bride.Father.Mother != null)
                {
                    bridesRelatives.Add(bride.Father.Mother);
                    bridesRelatives.Add(bride.Father.Father);
                }
            }
            if (groom.Mother != null)
            {
                //pull parents
                groomsRelatives.Add(groom.Mother);
                groomsRelatives.Add(groom.Father);

                //pull grandparents
                if (groom.Mother.Mother != null)
                {
                    groomsRelatives.Add(groom.Mother.Mother);
                    groomsRelatives.Add(groom.Mother.Father);
                }
                if (groom.Father.Mother != null)
                {
                    groomsRelatives.Add(groom.Father.Mother);
                    groomsRelatives.Add(groom.Father.Father);
                }
            }

            //compare the lists and if there are any duplicates, they are not compatible
            foreach(Member br in bridesRelatives)
            {
                foreach(Member gr in groomsRelatives)
                {
                    if (br.id == gr.id) return false;
                }
            }

            //two must be reasonably close in age
            int bridesAge = bride.GetAge(in_currentYear), groomsAge = groom.GetAge(in_currentYear);
            int elder, younger;

            if(bridesAge > groomsAge) {
                elder = bridesAge; younger = groomsAge;}
            else {
                elder = groomsAge; younger = bridesAge;}

            //using the formula if young > (old / 2) + 7
            if (younger > (elder / 2) + 7) return true;
            else return false;
        }

        public int GetAge(int in_currentYear)
        {
            return in_currentYear - YearOfBirth.mYear;
        }
        public MannerOfDeathGroup GetMannerOfDeath(Random random, int in_currentYear)
        {
            //NaturalCauses, Accident, Violence, Suicide,
            int age = GetAge(in_currentYear);
            int odds = random.Next(100);

            if(age < 10)
            {
                //85% - Natural Causes; 15% - Accident
                if (odds < 15) return MannerOfDeathGroup.Accident;
                else return MannerOfDeathGroup.NaturalCauses;
            }
            else if(age < 20)
            {
                //80% Natural Causes; 15% Accident; 5% Suicide
                if (odds < 5) return MannerOfDeathGroup.Suicide;
                else if (odds < 20) return MannerOfDeathGroup.Accident;
                else return MannerOfDeathGroup.NaturalCauses;
            }
            else if(age < 50)
            {
                //50% Natural Causes; 20% Accident; 20% Violence; 10% Suicide
                if (odds < 10) return MannerOfDeathGroup.Suicide;
                else if (odds < 30) return MannerOfDeathGroup.Violence;
                else if (odds < 50) return MannerOfDeathGroup.Accident;
                else return MannerOfDeathGroup.NaturalCauses;
            }
            else
            {
                //60% Natural Causes; 15% Accident; 15% Violence, 10% Suicide
                if (odds < 10) return MannerOfDeathGroup.Suicide;
                else if (odds < 25) return MannerOfDeathGroup.Violence;
                else if (odds < 40) return MannerOfDeathGroup.Accident;
                else return MannerOfDeathGroup.NaturalCauses;
            }
        }
        public static TwinGroup GetTwinGroup(Random random)
        {
            int odds = random.Next(100);

            if (odds < 50) return TwinGroup.Identical;
            else if (odds < 80) return TwinGroup.Fraternal;
            else return TwinGroup.Single;
        }
        public static GenderGroup GetGender(Random random)
        {
            if (random.Next(2) == 1) return GenderGroup.Female;
            else return GenderGroup.Male;
        }
        public static string GetFirstName(Random random, GenderGroup gender)
        {
            string[] MaleNames = new string[] { "Maxwell", "Leonardo", "Ashley", "Noah", "Daniel", "Benjamin", "Colin", "Lysander", "Harley", "Cornelius", "Ian", "Gabriel", "Arthur", "Marcus", "James", "Lorcan", "Maximilian", "Horatio", "Roscoe", "Barnabus", "Fergus", "Austen", "Hugo", "Winston", "Issac", "Pierce", "Dixon", "Ashby", "Sterling", "Charlie", "Arlington", "Oliver", "Porter", "Percy", "Rufus", "Andreas", "Ashfield", "Jonah", "Ellis", "Pierre", "Nicholas", "Chester", "Abbington", "Claus", "Quinn", "Lawrence", "Asher", "Giles", "David", "Theodore", "Valentine", "Jacob", "Julius", "Rafferty", "Lewis", "Stephen", "Orson", "Clayton", "Shamus", "Curtis", "Barnaby", "Harris", "Alexander", "Bradley", "Thomas", "Algerone", "Bruno", "Henry", "Harold", "Antoine", "Jackson", "Franklin", "Samuel", "Bishop", "Zachary", "Levi", "Ted", "Mansfeild", "Francis", "Leo", "Michael", "Hamilton", "Luca", "Hunter", "Carl", "Wentworth", "Horace", "Laurence", "Antony", "Roman", "Landon", "Clark", "Hector", "Kenneth", "Charles", "Wallace", "Dorian", "Harry", "Xavier", "Alastair", "Earl", "William", "Brody", "Crawford", "Christian", "Lawson", "Isadore", "Gideon", "Quinten", "Zacharias", "Clarence", "Maximillian", "Joseph", "Parker" };
            string[] FemaleNames = new string[] { "Josephina", "Arabella", "Geraldine", "Lily", "Marcella", "Francesca", "Ivy", "Theodora", "Miriam", "Lauren", "Yasmina", "Blanche", "Edwina", "Katherine", "Justine", "Catherine", "Helene", "Kathlyn", "Natalia", "Isobel", "Henrietta", "Amelie", "Araminta", "Rosalie", "Cynthia", "Leah", "Freya", "Jeanette", "Harper", "Stella", "Lorena", "Viola", "Saskia", "Jacqueline", "Paulette", "Vivian", "Frances", "Camille", "Gabrielle", "Camilla", "Harriet", "Henrieta", "Mary", "Phoebe", "Rosemary", "Adele", "Gwendoline", "Daphne", "Zara", "Audrey", "Elsie", "Margaret", "Cecilia", "Maisie", "Clarice", "Verity", "Luciana", "Alessandra", "Victoria", "Clara", "Georgina", "Patience", "Claudia", "Cecelia", "Georgiana", "Carolina", "Lila", "Kate", "Sophie", "Adelaide", "Helena", "Danielle", "Rose", "Matilda", "Belinda", "Francine", "Christiana", "Imogen", "Eliza", "Elaine", "Rosalind", "Miranda", "Octavia", "Genivive", "Bree", "Diane", "Evelynne", "Haraya", "Arabelle", "Persephone", "Layla", "Heather", "Evangeline", "Tabitha", "Cassandra", "Sylvia", "Muriel", "Gloria", "Clementine", "Veronica", "Sara", "Claire", "Bernice", "Mackayla", "Serafina", "Claudette", "Rowena", "Beatrice", "Isla" };

            if (gender == GenderGroup.Female)
                return FemaleNames[random.Next(FemaleNames.Length)];
            else return MaleNames[random.Next(MaleNames.Length)];
        }
        public static string GetLastName(Random random)
        {
            string[] LastNames = new string[] { "Wickes", "Anworth", "Lockwood", "Remington", "Wildingham", "Blakesley", "Chamberlain", "Sherington", "Hanover", "Windsor", "Christenson", "Down", "Angelo", "Morgan", "Garbert-Smithe", "Harvey-Lottway", "Danvers", "Digby", "Stone", "Tucker", "Bennet", "Musgrave", "Federline", "Brock", "Forge", "Kendall", "Kensington", "Rothchester", "Griggs", "Renaud", "Langston", "Fitzgerald", "Cook", "Farrell", "Lynch", "Farbridge", "Delafontaine", "Bronson", "Humphries", "DaVille", "Coleman", "Blackwood", "Harridan", "Riverty", "Exeter", "Ravenswood", "Beckett", "Montcroix", "Baxter", "Hillington", "Meyers", "Marfont", "Younger", "Gilkes", "Keswick", "Winchester", "Montgomery", "McLeod", "Addison", "Darlington-Whit", "Bentley", "Mortcombe", "Dixon", "Frinton-Smith", "Tennesley", "O'", "Travers", "Brewer", "Lockhart", "Westwood", "Mayer", "Millington", "Winfield", "Covington", "Evans", "Lennox", "Watson", "Jenkings", "Mavis", "Burton", "Brimsey", "Sheridan", "Wilson", "Cummings", "Charmant", "Keaton", "Findlay", "Thompkins", "Camden", "Kingsley", "Asquith", "May-Porter", "Clarkin", "Jenson", "Allencourt", "Ashleigh", "Stenham", "Hagan", "Crawford", "St.Claire", "Breckenridge", "Pierpont", "Tannenbay", "Abel", "Paxton", "Barnes", "Farraday", "Hillingham", "Richfield", "Rogers", "Conwyn", "Richmond", "Archer", "Wakefield", "Brent", "Ambrose", "Winston", "Lupton", "Rowley", "Finn", "Lovett", "Lockridge", "Claymoore", "Blythe", "Chins-Ranton", "Sinnett", "Seaton", "Robshaw", "Wilde", "Mercer", "Rutherford", "Rowe", "Alden", "Quentin", "Stuart-Lane", "Rothschild", "Pattinson", "Harrods", "Durchville", "Lennon", "Reed", "Whitehall", "Farrington", "Bryton", "Merriweather", "Spencer", "Upperton", "Greaves", "Leighton", "Hampton", "Frasier", "Beaumont", "Galashiels", "Gedge", "Forbes", "Davenport", "Milbourne", "Meyer", "Erickson", "Winthrope", "Wellington", "Astor", "St.Clair", "Mast", "Strain", "Smythe", "Benson", "Gainsborough", "Whitely", "Wells", "Garrington", "Stoneshire", "Jeffries", "Royale", "Redmond", "Hargreave", "Slater", "Belleville", "Mumford", "Marple", "Kaylock", "Dennison", "Belmont", "Atkins", "Lawson" };

            return LastNames[random.Next(LastNames.Length)];
        }
    }

    public class mDate
    {
        public int mYear, mMonth, mDay;
        public override string ToString()
        {
            return string.Format("{0}/{1}/{2}", mMonth, mDay, mYear);
        }
        
        public static mDate ReturnRandomBirthday(Random random, int birthYear)
        {
            mDate d = new mDate() { mYear = birthYear, mMonth = random.Next(1, 13) };

            if (d.mMonth == 2) // determine number of days in February
            {
                //if it's a leapyear
                if (d.mYear % 4 == 0 && d.mYear % 100 != 0)
                {
                    d.mDay = random.Next(1, 30);
                }
                else d.mDay = random.Next(1, 29);
            }
            else if (d.mMonth == 4 || d.mMonth == 6 || d.mMonth == 9 || d.mMonth == 11) d.mDay = random.Next(1, 31);
            else d.mDay = random.Next(1, 32);

            return d;
        }
    }
}
