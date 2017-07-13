using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Humanizer;
using Newtonsoft.Json;
using NLipsum.Core;
using Octopus.Client;
using Octopus.Client.Editors.Async;
using Octopus.Client.Model.DeploymentProcess;
using Octopus.Client.Model;
using Octopus.Client.Model.Endpoints;
using Octopus.Sampler.Infrastructure;
using Serilog;

namespace Octopus.Sampler.Commands
{
    [Command("multitenant-sample", Description = "Applies the multi-tenant deployments sample.")]
    public class MultiTenantSampleCommand : ApiCommand
    {
        private static readonly string[] ProjectNames = { "Synergy", "Mojo", "Chromatopathic", "Equianharmonic", "Interlocation", "Vell", "Complicity", "Intramorainic", "Coccolithophoridae", "Unrecuperated", "Conservational", "Variolic", "Cardiosphygmograph", "Hysteroproterize", "Playwriter", "Mongibel", "Alternize", "Benzothiodiazole", "Neuroptera", "Nontarnishable", "Intermorainic", "Disaccord", "Fumeroot", "Dissenting", "Voyeurism", "Procatarctic", "Teratogenous", "Immovability", "Cardiorespiratory", "Venomous", "Longish", "Outkill", "Cyclonometer", "Homochromatism", "Adenographic", "Bacteritic", "Piassava", "Mysian", "Blastophaga", "Acromiothoracic", "Gastrolytic", "Diapnotic", "Interassure", "Bureaucracy", "Belated", "Peronium", "Slopselling", "Oversmall", "Mariner", "Swift", "Spined", "Protovertebra", "Goadsman", "Unplacable", "Metapsychology", "Thalassometer", "Duplicand", "Hypogenetic", "Undistractingly", "Bravadoism", "Trichopathic", "Uncapsized", "Chrismary", "Nephrapostasis", "Chupak", "Entailer", "Solo", "Tobaccoman", "Plasticine", "Delabialization", "Aminoglutaric", "Goldbird", "Thermae", "Pleurotremata", "Nonforeign", "Mindsight", "Platycercus", "Transmittancy", "Frogeye", "Incrassated", "Peridentoclasia", "Interregnal", "Intraseminal", "Mesomyodi", "Sinaean", "Brute", "Unsecuredly", "Composed", "Analogy", "Brat", "Affray", "Moslemah", "Homonymy", "Endostracal", "Nuggar", "Chaffinch", "Bumbailiffship", "Grammaticaster", "Undignifiedness", "Polypotome", "Relinquish", "Botchiness", "Totalizator", "Unsuspecting", "Slaum", "Dismally", "Overearnestness", "Hexahydrate", "Gingerness", "Spectromicroscope", "Unknittable", "Urethrosexual", "Qualmish", "Bucolical", "Slimishness", "Oraculous", "Sociability", "Crunchable", "Infraclavicle", "Noncorrosive", "Demioctagonal", "Endoperidium", "Agitable", "Respectant", "Emotionlessness", "Gigelira", "Megalokaryocyte", "Thymectomize", "Smoothingly", "Hypotremata", "Conversative", "Thrillingness", "Hypertype", "Onychophagist", "Ungenial", "Caltha", "Demegoric", "Eelbob", "Metallik", "Slipshodness", "Thermoluminescent", "Schonfelsite", "Organonomy", "Inofficial", "Masonry", "Victim", "Hyperdemocracy", "Empirical", "Muranese", "Balbutiate", "Muscovy", "Calenderer", "Autoformation", "Lap", "Docible", "Inbuilt", "Unquietly", "Nonconcurrent", "Vasquine", "Bradyphasia", "Emgalla", "Tribromophenol", "Tibiometatarsal", "Periphrase", "Imparter", "Intending", "Multicoil", "Plexometer", "Rubricator", "Lifting", "Anencephalous", "Hah", "Kakkak", "Saurognathous", "Kickout", "Heterogalactic", "Tasheriff", "Degradation", "Wasteness", "Cylindroconoidal", "Cassena", "Fibrolite", "Bicostate", "Untumultuous", "Isawa", "Sacrificer", "Kudize", "Hexahydrated", "Pentagon", "Indispose", "Scurrility", "Sightliness", "Preacknowledge", "Ungiant", "Conveyance", "Chordacentrum", "Resentingly", "Pedicel", "Unlatch", "Mayance", "Publisher", "Steeve", "Niggerhead", "Natchitoches", "Rattlepate", "Journeycake", "Shadowed", "Polyglotter", "Spinney", "Ophthalmocele", "Arsenophenol", "Antepreterit", "Antidemocratic", "Submersed", "Saddled", "Macromastia", "Bacterioprotein", "Latus", "Unloving", "Hepatolenticular", "Taxine", "Bitentaculate", "Solubility", "Comber", "Intort", "Comortgagee", "Mesiad", "Septifolious", "Capillarily", "Antifreezing", "Pyovesiculosis", "Tithonic", "Uneloped", "Dite", "Marquetry", "Panhyperemia", "Hudibras", "Promotiveness", "Hypocarpium", "Zephyr", "Dornock", "Lithodomous", "Feminin", "Interstate", "Expressness", "Epistrophic", "Coronule", "Sexennially", "Pyknatom", "Archicyte", "Blouse", "Retropharyngitis", "Presplendor", "Withdrawal", "Trumpetbush", "Cassumunar", "Regental", "Archcozener", "Ultraplanetary", "Tailorization", "Preaccumulate", "Nassology", "Profusely", "Gonalgia", "Plumbet", "Procoelous", "Sporangioid", "Postclavicula", "Rhyotaxitic", "Boatsman", "Anticovenanting", "Beice", "Dinornithine", "Unringed", "Hormonopoiesis", "Unconsumptive", "Undistant", "Additivity", "Uncultivable", "Nisus", "Akonge", "Profanchise", "Darning", "Lernaean", "Ungoverned", "Palaeodictyopteran", "Nakula", "Arthrosclerosis", "Beneficence", "Hopyard", "Vick", "Gospelize", "Bronchomucormycosis", "Poetship", "Catjang", "Fetishize", "Mydine", "Acheirous", "Cubist", "Fardo", "Adoptianist", "Megaric", "Mendipite", "Aldehydrol", "Psychrophilic", "Sequa", "Intercompany", "Jawed", "Antienzymic", "Contradictory", "Enambush", "Knifeproof", "Antitrades", "Nototherium", "Neurophagy", "Excrementitiously", "Uncorseted", "Mantology", "Sympathetic", "Unserious", "Plasmatic", "Archesporial", "Whiggish", "Semitransparency", "Waistcoated", "Sachemship", "Warrior", "Iridin", "Disanimal", "Jeoparder", "Trochilic", "Depressiveness", "Pericarditis", "Mariolater", "Unadjacent", "Rottock", "Coroniform", "Import", "Daphnin", "Cardiacal", "Sublimational", "Boccale", "Incorruptibility", "Invalescence", "Intraduodenal", "Passulate", "Drivelingly", "Paleaceous", "Coadjacent", "Flanger", "Unlikable", "Autogiro", "Unstoniness", "Approvement", "Inadequatively", "Seginus", "Equivoque", "Noxally", "Nyctanthes", "Impartiality", "Empyrean", "Melismatic", "Theromores", "Quadrilobate", "Antigenicity", "Intertrude", "Underact", "Subaqueous", "Stowwood", "Sacrification", "Defecation", "Phylactolaema", "Reformingly", "Gamekeeping", "Unfunctional", "Mountainet", "Ephemera", "Monarticular", "Burningly", "Hypsistenocephalic", "Crocodylus", "Awe", "Vertebrated", "Crookfingered", "Glozing", "Towerwise", "Cyclopedical", "Soh", "Northernize", "Radiotelephony", "Somaticosplanchnic", "Traditionist", "Neomenia", "Nonprorogation", "Creditress", "Gametogony", "Bonelet", "Servomechanism", "Bracketwise", "Chauceriana", "Recontribution", "Ascolichenes", "Ebb", "Horometry", "Lathyrus", "Protovestiary", "Passagian", "Ptyalectasis", "Mediatorialism", "Syllabe", "Couched", "Zygophyllum", "Tickling", "Applosive", "Amnestic", "Arthrodonteae", "Complected", "Cryptorrhetic", "Azotic", "Stability", "Silicomethane", "Spunkie", "Wramp", "Faddist", "Overstudy", "Polyzoary", "Ankylostoma", "Lombard", "Photographee", "Zygomaticoauricularis", "Youthhead", "Pointy", "Thalian", "Podosphaera", "Declimatize", "Promorphology", "Deworm", "Craneway", "Mensurative", "Synarmogoidea", "Rach", "Tentaculum", "Antennular", "Thereanents", "Monoplacula", "Subresin", "Smirkly", "Propale", "Petrifactive", "Signification", "Anesthesiant", "Gibus", "Snailfish", "Hystricine", "Akhmimic", "Affably", "Deserveless", "Peritoneotomy", "Czechization", "Thunderstroke", "Asphodelus", "Intoxicatedly", "Lifehold", "Unilaterality", "Scantness", "Napierian", "Buxus", "Equinox", "Skimpily", "Paragastral", "Lymphatic", "Trigonon", "Derangeable", "Thylacoleo", "Brancher", "Preadulthood", "Digametic", "Primosity", "Pilori", "Viburnin", "Splayed", "Apicifixed", "Flamelet", "Trachealgia", "Secancy", "Cryptophthalmos", "Aeoline", "Underaim", "Ichthyopsida", "Rhapsode", "Maledict", "Unhelmeted", "Phytosociological", "Overexpert", "Bulldoze", "Understain", "Passably", "Splendiferously", "Unfinable", "Fornax", "Connaught", "Gainfulness", "Soldierhood", "Fork", "Prelatish", "Investigating", "Outmalaprop", "Iterativeness", "Spitted", "Aircrew", "Schistosomiasis", "Hypogynous", "Unvaluable", "Premunitory", "Overcontentment", "Culinarily", "Werent", "Cryptic", "Oschophoria", "Atramentous", "Clavelize", "Dyssystole", "Vestalship", "Trinket", "Anamorphic", "Charlatanistic", "Diversified", "Ascomycetes", "Spawneater", "Courageous", "Gametophore", "Terseness", "Unencouraging", "Duodenum", "Framer", "Distracter", "Unafflictedly", "Kalmarian", "Pelvis", "Paniculated", "Reluctate", "Electroluminescent", "Londinensian", "Weddinger", "Nowed", "Demarkation", "Unglosed", "Nonsensicality", "Oncidium", "Ceylanite", "Extradotal", "Concentricity", "Nonobservation", "Monograptid", "Nondictionary", "Glossolalia", "Semimajor", "Generalship", "Bastaard", "Poultrylike", "Lactide", "Surroundings", "Requiescence", "Unflaggingly", "Inferentially", "Affect", "Novem", "Allah", "Brontoscopy", "Diastataxy", "June", "Nonheritor", "Celeriac", "Aguinaldo", "Opodymus", "Darshana", "Tenancy", "Antienzyme", "Bibliofilm", "Agapanthus", "Propago", "Saip", "Frugal", "Eusol", "Lilliputian", "Tetrical", "Ethnographist", "Sanctionative", "Bromocyanide", "Meloe", "Insanity", "Dankness", "Spicous", "Measurable", "Sterile", "Gilling", "Sharepenny", "Flyweight", "Exoccipital", "Vulcanic", "Trolleyman", "Lagetto", "Reproachableness", "Lumberingly", "Gonium", "Aggravating", "Isosporic", "Orchidopexy", "Pacchionian", "Imprevisibility", "Psychodynamics", "Reduplicature", "Moonack", "Thiosinamine", "Amicronucleate", "Leucoplakia", "Brandisite", "Underpoint", "Poonga", "Impact", "Raconteur", "Revealment", "Savanilla", "Fondlike", "Ostentive", "Pennant", "Pneumatometry", "Tradeswoman", "Nondesisting", "Spectrobolometric", "Romanceless", "Bandrol", "Evert", "Chondroganoidei", "Rhyparography", "Serovaccine", "Watchmanship", "Pookaun", "Cocama", "Tonsillectome", "Drugshop", "Mesmerization", "Trailingly", "Homosexualism", "Halichondroid", "Anisopia", "Nonhereditary", "Sesquiquarta", "Hemipenis", "Steganophthalmata", "Corybantiasm", "Fany", "Portendment", "Paleobotanical", "Dissiliency", "Wainbote", "Dermatotherapy", "Childness", "Reawaken", "Postmillenarianism", "Mewl", "Salpinx", "Referendum", "Unreconcilableness", "Ruddleman", "Negroidal", "Monocarbonic", "Aviatic", "Orgeat", "Vivisectionally", "Walach", "Bromacetic", "Hieronymic", "Hermetics", "Passenger", "Rigmarolery", "Eburnation", "Dromiceius", "Bram", "Infinitival", "Excessman", "Ducally", "Unplacid", "Unprepared", "Craniognosy", "Amidoazobenzene", "Housewear", "Noncommemoration", "Elderwort", "Diversification", "Sarcine", "Misconjecture", "Overtimer", "Rubblework", "Smarmy", "Irenic", "Melanorrhoea", "Subsextuple", "Budget", "Peneplane", "Pteraspis", "Uranoscopic", "Beheld", "Reconstitution", "Tenderee", "Sonnetary", "Beflout", "Gastroenterocolitis", "Scrimshorn", "Curatial", "Semisensuous", "Chipping", "Vermeil", "Gelandelaufer", "Unvouchsafed", "Coaudience", "Mizzen", "Pumpkin", "Gynecophoric", "Hondurean", "Schemery", "Centrosphere", "Dugong", "Preconsolation", "Server", "Abyssolith", "Shipyard", "Solidarist", "Disillusionizer", "Rainstorm", "Verdurous", "Overgird", "Flagellated", "Deglaze", "Dentistical", "Foolery", "Presuitability", "Pulmonifer", "Oenophobist", "Seleniferous", "Comital", "Regrowth", "Contestant", "Accidentiality", "Untread", "Capsaicin", "Quinone", "Talcochlorite", "Trappist", "Dumb", "Acanthosis", "Zerumbet", "Besleeve", "Phlebotomize", "Roughometer", "Displease", "Arsyl", "Childe", "Piper", "Semateme", "Respectableness", "Stinker", "Phrontistery", "Pume", "Tetterish", "Brutification", "Laccainic", "Mermaid", "Restitutionist", "Olpe", "Puntal", "Sternoglossal", "Macrocystis", "Indagative", "Befit", "Intelligentsia", "Bronchoconstriction", "Metritis", "Pycnia", "Ahura", "Osteochondrophyte", "Hysteromyoma", "Holler", "Shaviana", "Daphnia", "Ethmovomer", "Metewand", "Psychostasy", "Dreepy", "Reannouncement", "Anticonfederationist", "Isoxazine", "Ashpan", "Carpetbaggism", "Snails", "Undistractedly", "Mercuriammonium", "Backswordman", "Acknowledge", "Eurypygidae", "Immanacle", "Postrubeolar", "Seebeck", "Nonresponsibility", "Subterraneanly", "Premorally", "Unchiming", "Awave", "Protonitrate", "Addlings", "Remortgage", "Unsanded", "Damia", "Levotartaric", "Sweepstake", "Ratti", "Shipkeeper", "Embryologically", "Erogenetic", "Vaporous", "Puissant", "Unquarreled", "Yapper", "Unfixing", "Chloroaurate", "Carpophalangeal", "Leucism", "Visionproof", "Rustication", "Functionalize", "Hyacinthia", "Platyptera", "Anthrathiophene", "Prussification", "Quatuor", "Plagiaristically", "Inlaid", "Monothelete", "Libant", "Daltonist", "Arborist", "Nonevolutionist", "Fossoria", "Aquiform", "Stigmaticalness", "Schisandraceae", "Gallotannate", "Clausiliidae", "Ergophobiac", "Discohexaster", "Hypotonic", "Tergite", "Nongraduated", "Meningocephalitis", "Anisostomous", "Trustwoman", "Ceriornis", "Amphicarpous", "Isoagglutinogen", "Scleroticectomy", "Swath", "Womanism", "Youthlike", "Detrition", "Valid", "Palaeoniscid", "Zero", "Adjective", "Glycyrrhiza", "Thiodiphenylamine", "Misarrangement", "Double", "Odontoblast", "Thoracostracan", "Licorice", "Sulphoterephthalic", "Semispiritous", "Preconscious", "Resistible", "Woman", "Undertutor", "Canman", "Tympanosis", "Tithable", "Contreface", "Sobranje", "Analyzability", "Blubberer", "Zygoma", "Nonreprehensible", "Vim", "Cuddly", "Dermatomuscular", "Theophilanthrope", "Circumcentral", "Nonmanufacture", "Prosodetic", "Unbuffered", "Understudy", "Rel", "Monkeyhood", "Manchineel", "Darky", "Hypidiomorphic", "Quill", "Unsublimed", "Deforester", "Unassibilated", "Nondeportation", "Rhipidoglossate", "Heedlessness", "Compromit", "Rarefactive", "Unmasking", "Reconditeness", "Vallevarite", "Elucubrate", "Lilt", "Perspective", "Untribal", "Swizzle", "Bibliotaphic", "Myriosporous", "Trellislike", "Chocho", "Universalism", "Terephthalic", "Inactivate", "Diffusively", "Monopersonal", "Rinner", "Jehovah", "Sceloporus", "Numerist", "Upstartness", "Postarticular", "Chemiatric", "Woolwa", "Malkite", "Evilness", "Vertices", "Mopingly", "Vermicule", "Stigmatiform", "Chay", "Echinomys", "Triplochitonaceae", "Antiprostate", "Browbeat", "Gigartina", "Preconcentratedly", "Neuralgy", "Visuopsychic", "Noncommercial", "Superinnocent", "Palisado", "Dermasurgery", "Culminate", "Unmolded", "Russophobiac", "Chrisroot", "Jungermanniaceae", "Revigorate", "Lanneret", "Parablepsis", "Placodermal", "Orbless", "Precoloration", "Unhocked", "Leukocidin", "Charruan", "Biggonet", "Semidelirious", "Phasemeter", "Erythrol", "Nutjobber", "Paterissa", "Akroterion", "Eyalet", "Overmature", "Refilm", "Deicide", "Mirfak", "Goosishness", "Wagling", "Geosyncline", "Wondermonger", "Reapproach", "Palaeoeremology", "Noreaster", "Rhabdite", "Elocutionary", "Abominable", "Otocerebritis", "Heldentenor", "Papaship", "Intractability" };
        private static readonly LipsumGenerator LipsumTheRaven = new LipsumGenerator(Lipsums.TheRaven, isXml: false);
        private static readonly LipsumGenerator LipsumRobinsonoKruso = new LipsumGenerator(Lipsums.RobinsonoKruso, isXml: false);

        private const int DefaultNumberOfProjects = 20;
        private const int DefaultNumberOfCustomers = 50;
        private const int DefaultNumberOfTesters = 10;

        private static readonly ILogger Log = Serilog.Log.ForContext<MultiTenantSampleCommand>();

        public MultiTenantSampleCommand(IOctopusClientFactory octopusClientFactory)
            : base(octopusClientFactory)
        {
            var options = Options.For("Multi-tenant sample");
            options.Add("projects=", $"[Optional] Number of projects to create, default {DefaultNumberOfProjects}", v => NumberOfProjects = int.Parse(v));
            options.Add("customers=", $"[Optional] Number of customer tenants to create, default {DefaultNumberOfCustomers}", v => NumberOfCustomers = int.Parse(v));
            options.Add("testers=", $"[Optional] Number of test tenants to create, default {DefaultNumberOfTesters}", v => NumberOfTesters = int.Parse(v));
        }

        public int NumberOfProjects { get; protected set; } = DefaultNumberOfProjects;
        public int NumberOfCustomers { get; protected set; } = DefaultNumberOfCustomers;
        public int NumberOfTesters { get; protected set; } = DefaultNumberOfTesters;

        public static class VariableKeys
        {
            public static class StandardTenantDetails
            {
                public static readonly string TenantAlias = "Tenant.Alias";
                public static readonly string TenantRegion = "Tenant.Region";
                public static readonly string TenantContactEmail = "Tenant.ContactEmail";
            }

            public static class ProjectTenantVariables
            {
                public static readonly string TenantDatabasePassword = "Tenant.Database.Password";
            }
        }

        public class Region
        {
            public Region(string @alias, string displayName)
            {
                Alias = alias;
                DisplayName = displayName;
            }

            public string Alias { get; set; }
            public string DisplayName { get; set; }

            public static Region[] All =
            {
                new Region("AustraliaEast", "Australia East"),
                new Region("SoutheastAsia", "South East Asia"),
                new Region("WestUS", "West US"),
                new Region("EastUS", "East US"),
                new Region("WestEurope", "West Europe"),
            };
        }

        protected override async Task Execute()
        {
            if (NumberOfProjects >= ProjectNames.Length)
                throw new CommandException($"Please create up to {ProjectNames.Length} projects, we only have so many random names!");

            Log.Information("Building multi-tenant sample with {ProjectCount} projects, {CustomerCount} customers and {TesterCount} testers...",
                NumberOfProjects, NumberOfCustomers, NumberOfTesters);

            await EnsureMultitenancyFeature();

            Log.Information("Setting up environments...");
            var allEnvironmentsTasks = new[] {"MT Dev", "MT Test", "MT Beta", "MT Staging", "MT Production"}
                .Select(name => Repository.Environments.CreateOrModify(name, LipsumRobinsonoKruso.GenerateLipsum(1)))
                .ToArray();
            var allEnvironments = (await Task.WhenAll(allEnvironmentsTasks)).Select(e => e.Instance).ToArray();
            var normalLifecycle = await Repository.Lifecycles.CreateOrModify("MT Normal Lifecycle", "The normal lifecycle for the multi-tenant deployments sample");
            await normalLifecycle.AsSimplePromotionLifecycle(allEnvironments.Where(e => e.Name != "MT Beta").ToArray()).Save();
            var betaLifecycle = await Repository.Lifecycles.CreateOrModify("MT Beta Lifecycle", "The beta lifecycle for the multi-tenant deployments sample");
            await betaLifecycle.AsSimplePromotionLifecycle(allEnvironments.Take(3).ToArray()).Save();
            var projectGroup = await Repository.ProjectGroups.CreateOrModify("Multi-tenancy sample");

            Log.Information("Setting up tags...");
            var tagSetTenantType = await Repository.TagSets.CreateOrModify("Tenant type", "Allows you to categorize tenants");
            await tagSetTenantType.AddOrUpdateTag("Internal", "These are internal tenants, like our test team")
                .AddOrUpdateTag("External", "These are external tenants, our real customers", TagResource.StandardColor.LightBlue)
                .Save();

            var tagSetImportance = await Repository.TagSets.CreateOrModify("Tenant importance", "Allows you to have different customers that we should pay more or less attention to");
            await tagSetImportance.AddOrUpdateTag("VIP", "Very important tenant - pay attention!", TagResource.StandardColor.DarkRed)
                .AddOrUpdateTag("Standard", "These are our standard customers")
                .AddOrUpdateTag("Trial", "These are trial customers", TagResource.StandardColor.DarkPurple)
                .Save();

            var tagSetRing = await Repository.TagSets.CreateOrModify("Upgrade ring", "What kind of upgrade stability to these customers want");
            await tagSetRing.AddOrUpdateTag("Tester", "These are our internal test team members", TagResource.StandardColor.LightCyan)
                .AddOrUpdateTag("Early adopter", "Upgrade these customers first", TagResource.StandardColor.LightYellow)
                .AddOrUpdateTag("Stable", "Upgrade these customers last", TagResource.StandardColor.LightGreen)
                .AddOrUpdateTag("Pinned", "Don't upgrade these customers until they come back to the stable ring", TagResource.StandardColor.DarkRed)
                .Save();

            var tagSetEarlyAccessProgram = await Repository.TagSets.CreateOrModify("Early access program", "Provides tenants with access to certain early access programs.");
            await tagSetEarlyAccessProgram.AddOrUpdateTag("2.x Beta", "These customers are part of our 2.x beta program", TagResource.StandardColor.LightPurple)
                .Save();

            var tagSetHosting = await Repository.TagSets.CreateOrModify("Hosting", "Allows you to define where the tenant software should be hosted");
            await tagSetHosting.AddOrUpdateTag("Internal-Shared-Farm", "The internal test server farm")
                .AddOrUpdateTag("Shared-Farm-1", "Shared server farm 1", TagResource.StandardColor.LightGreen)
                .AddOrUpdateTag("Shared-Farm-2", "Shared server farm 2", TagResource.StandardColor.LightGreen)
                .AddOrUpdateTag("Shared-Farm-3", "Shared server farm 3", TagResource.StandardColor.LightGreen)
                .AddOrUpdateTag("Shared-Farm-4", "Shared server farm 4", TagResource.StandardColor.LightGreen)
                .AddOrUpdateTag("Dedicated", "This customer will have their own dedicated hardware", TagResource.StandardColor.DarkRed)
                .Save();

            var allTags = new TagResource[0]
                .Concat(tagSetTenantType.Instance.Tags)
                .Concat(tagSetHosting.Instance.Tags)
                .Concat(tagSetImportance.Instance.Tags)
                .Concat(tagSetRing.Instance.Tags)
                .Concat(tagSetEarlyAccessProgram.Instance.Tags)
                .ToArray();

            var getTag = new Func<string, TagResource>(name => allTags.FirstOrDefault(t => t.Name == name));

            Log.Information("Setting up the untenanted host for the development...");
            var untenantedHosts = Enumerable.Range(0, 1).Select(i => Repository.Machines.CreateOrModify(
                $"Untenanted Node {i}",
                new CloudRegionEndpointResource(),
                allEnvironments.Where(e => e.Name == "MT Dev").ToArray(),
                new[] { "web-server" }))
                .ToArray();

            Log.Information("Setting up the shared hosts for the test environment...");
            foreach (var internalSharedHostTag in tagSetHosting.Instance.Tags.Where(t => t.Name.StartsWith("Internal-Shared")))
            {
                var sharedHosts = Enumerable.Range(0, 4).Select(i => Repository.Machines.CreateOrModify(
                    $"{internalSharedHostTag.Name} Node {i}",
                    new CloudRegionEndpointResource(),
                    allEnvironments.Where(e => e.Name == "MT Test").ToArray(),
                    new[] { "web-server" },
                    new TenantResource[0],
                    new[] { internalSharedHostTag },
                    TenantedDeploymentMode.Tenanted))
                    .ToArray();
            }

            Log.Information("Setting up the shared hosts for the production environment...");
            foreach (var sharedHostTag in tagSetHosting.Instance.Tags.Where(t => t.Name.StartsWith("Shared-Farm")))
            {
                await Task.WhenAll(
                    Enumerable.Range(0, 4)
                        .Select(i => Repository.Machines.CreateOrModify(
                            $"{sharedHostTag.Name} Node {i}",
                            new CloudRegionEndpointResource(),
                            allEnvironments.Where(e => e.Name == "MT Production").ToArray(),
                            new[] { "web-server" },
                            new TenantResource[0],
                            new[] { sharedHostTag },
                            TenantedDeploymentMode.Tenanted))
                        .ToArray()
                );
            }

            Log.Information("Setting up variables...");
            var envVarEditor = await Repository.LibraryVariableSets.CreateOrModify("Environment variables", "The environment details we require for all projects");
            foreach (var e in allEnvironments)
            {
                (await envVarEditor.Variables).AddOrUpdateVariableValue("Environment.Alias", e.Name.Replace("MT ", "").ToLowerInvariant(), new ScopeSpecification { { ScopeField.Environment, e.Id } });
            }
            await envVarEditor.Save();

            var stdTenantVarEditor = await Repository.LibraryVariableSets.CreateOrModify("Standard tenant details", "The standard details we require for all tenants");
            stdTenantVarEditor.VariableTemplates
                .AddOrUpdateSingleLineTextTemplate(VariableKeys.StandardTenantDetails.TenantAlias, "Alias", defaultValue: null, helpText: "This alias will be used to build convention-based settings for the tenant")
                .AddOrUpdateSelectTemplate(VariableKeys.StandardTenantDetails.TenantRegion, "Region", Region.All.ToDictionary(x => x.Alias, x => x.DisplayName), defaultValue: null, helpText: "The geographic region where this tenant will be hosted")
                .AddOrUpdateSingleLineTextTemplate(VariableKeys.StandardTenantDetails.TenantContactEmail, "Contact email", defaultValue: null, helpText: "A comma-separated list of email addresses to send deployment notifications");
            await stdTenantVarEditor.Save();

            var libraryVariableSets = new[] { envVarEditor.Instance, stdTenantVarEditor.Instance };

            Log.Information("Building {Count} sample projects...", NumberOfProjects);
            var projects = await Task.WhenAll(
                    Enumerable.Range(0, NumberOfProjects)
                    .Select(i => new { Name = ProjectNames[i], Alias = ProjectNames[i].ToLowerInvariant() })
                    .Select(async (x, i) =>
                    {
                        Log.Information("Setting up project {ProjectName}...", x.Name);
                        var projectEditor = await Repository.Projects.CreateOrModify(x.Name, projectGroup.Instance, normalLifecycle.Instance, LipsumTheRaven.GenerateLipsum(2));
                        projectEditor.IncludingLibraryVariableSets(libraryVariableSets);

                        var image = SampleImageCache.GetRobotImage(x.Name);
                        if (!string.IsNullOrWhiteSpace(image))
                            await projectEditor.SetLogo(image);

                        projectEditor.VariableTemplates
                            .Clear()
                            .AddOrUpdateSingleLineTextTemplate("Tenant.Database.Name", "Database name", $"{x.Alias}-#{{Environment.Alias}}-#{{Tenant.Alias}}", $"The environment-specific name of the {x.Name} database for this tenant.")
                            .AddOrUpdateSingleLineTextTemplate("Tenant.Database.UserID", "Database username", $"{x.Alias}-#{{Environment.Alias}}-#{{Tenant.Alias}}", "The User ID used to connect to the tenant database.")
                            .AddOrUpdateSensitiveTemplate(VariableKeys.ProjectTenantVariables.TenantDatabasePassword, "Database password", defaultValue: null, helpText: "The password used to connect to the tenant database.")
                            .AddOrUpdateSingleLineTextTemplate("Tenant.Domain.Name", "Domain name", $"#{{Tenant.Alias}}.{x.Alias}.com", $"The environment-specific domain name for the {x.Name} web application for this tenant.");

                        (await projectEditor.Variables)
                            .AddOrUpdateVariableValue("DatabaseConnectionString", $"Server=db.{x.Alias}.com;Database=#{{Tenant.Database.Name}};User ID=#{{Tenant.Database.UserID}};Password=#{{Tenant.Database.Password}};")
                            .AddOrUpdateVariableValue("HostURL", "https://#{Tenant.Domain.Name}");

                        // Create the channels for the sample project
                        var channel = await projectEditor.Channels.CreateOrModify("1.x Normal", "The channel for stable releases that will be deployed to our production customers.");
                            await channel.SetAsDefaultChannel()
                                .AddOrUpdateTenantTags(getTag("Tester"), getTag("Early adopter"), getTag("Stable"))
                                .Save();
                        var betaChannelEditor = await projectEditor.Channels.CreateOrModify("2.x Beta", "The channel for beta releases that will be deployed to our beta customers.");
                            betaChannelEditor.UsingLifecycle(betaLifecycle.Instance)
                            .AddOrUpdateTenantTags(getTag("2.x Beta"));

                        // Delete the "default channel" if it exists
                        await projectEditor.Channels.Delete("Default");

                        // Rebuild the process from scratch
                        var deploymentProcess = await projectEditor.DeploymentProcess;
                        deploymentProcess.ClearSteps();

                        deploymentProcess.AddOrUpdateStep("Deploy Application")
                            .TargetingRoles("web-server")
                            .AddOrUpdateScriptAction("Deploy Application", new InlineScriptActionFromFileInAssembly("MultiTenantSample.Deploy.ps1"), ScriptTarget.Target);

                        deploymentProcess.AddOrUpdateStep("Deploy 2.x Beta Component")
                            .TargetingRoles("web-server")
                            .AddOrUpdateScriptAction("Deploy 2.x Beta Component", new InlineScriptActionFromFileInAssembly("MultiTenantSample.DeployBetaComponent.ps1"), ScriptTarget.Target)
                            .ForChannels(betaChannelEditor.Instance);

                        deploymentProcess.AddOrUpdateStep("Notify VIP Contact")
                            .AddOrUpdateScriptAction("Notify VIP Contact", new InlineScriptActionFromFileInAssembly("MultiTenantSample.NotifyContact.ps1"), ScriptTarget.Server)
                            .ForTenantTags(getTag("VIP"));

                        projectEditor.Instance.TenantedDeploymentMode = TenantedDeploymentMode.Tenanted;

                        await projectEditor.Save();

                        return projectEditor.Instance;
                    })
                .ToArray()
            );

            Log.Information("Building {Count} sample Testers...", NumberOfTesters);
            var testers = await Task.WhenAll(
                GetSampleTenantData("Tester", NumberOfTesters)
                    .Select(async x =>
                    {
                        Log.Information("Setting up tester {TenantName}...", x.Name);
                        var tenantEditor = await Repository.Tenants.CreateOrModify(x.Name);
                        await tenantEditor.SetLogo(SampleImageCache.DownloadImage(x.LogoUrl));
                        tenantEditor
                            .WithTag(getTag("Internal"))
                            .WithTag(getTag("Tester"))
                            .WithTag(getTag("Internal-Shared-Farm"))
                            .WithTag(getTag("2.x Beta"));

                        // Connect to projects/environments
                        tenantEditor.ClearProjects();
                        var testEnvironments = allEnvironments.Where(e => e.Name == "MT Test").ToArray();
                        foreach (var project in projects)
                        {
                            tenantEditor.ConnectToProjectAndEnvironments(project, testEnvironments);
                        }
                        await tenantEditor.Save();

                        // Ensure project mapping is saved before we attempt to fill out variables - otherwise they won't exist
                        await FillOutTenantVariablesByConvention(tenantEditor, projects, allEnvironments, libraryVariableSets);

                        await tenantEditor.Save();
                        return tenantEditor.Instance;
                    })
                    .ToArray()
            );

            Log.Information("Building {Count} sample Customers...", NumberOfCustomers);
            var customers = await Task.WhenAll(
                GetSampleTenantData("Customer", NumberOfCustomers)
                    .Select(async x =>
                    {
                        Log.Information("Setting up customer {TenantName}...", x.Name);
                        var tenantEditor = await Repository.Tenants.CreateOrModify(x.Name);
                        await tenantEditor.SetLogo(SampleImageCache.DownloadImage(x.LogoUrl));
                        TagCustomerByConvention(tenantEditor.Instance, allTags);

                        // Connect to projects/environments
                        var customerEnvironments = GetEnvironmentsForCustomer(allEnvironments, tenantEditor.Instance);
                        foreach (var project in projects)
                        {
                            tenantEditor.ConnectToProjectAndEnvironments(project, customerEnvironments);
                        }
                        await tenantEditor.Save();

                        // Ensure project mapping is saved before we attempt to fill out variables - otherwise they won't exist
                        await FillOutTenantVariablesByConvention(tenantEditor, projects, allEnvironments, libraryVariableSets);
                        await tenantEditor.Save();
                        return tenantEditor.Instance;
                    })
                    .ToArray()
            );

            foreach (var customer in customers.Where(c => c.IsVIP()))
            {
                Log.Information("Setting up dedicated hosting for {VIP}...", customer.Name);
                await Task.WhenAll(
                    Enumerable.Range(0, 2)
                        .Select(i => Repository.Machines.CreateOrModify(
                            $"{customer.Name} Host {i}",
                            new CloudRegionEndpointResource(),
                            GetEnvironmentsForCustomer(allEnvironments, customer),
                            new[] {"web-server"},
                            new[] {customer},
                            new TagResource[0],
                            TenantedDeploymentMode.Tenanted))
                        .ToArray()
                );
            }

            Log.Information("Created {CustomerCount} customers and {TesterCount} testers and {ProjectCount} projects.",
                customers.Length, testers.Length, projects.Length);
            Log.Information("Customer tagging conventions: Names with 'v' will become 'VIP' (with dedicated hosting), names with 'u' will become 'Trial', names with 'e' will become 'Early adopter', everyone else will be 'Standard' and assigned to a random shared server pool.");
        }

        async Task EnsureMultitenancyFeature()
        {
            Log.Information("Ensuring multi-tenant deployments are enabled...");
            var features = await Repository.FeaturesConfiguration.GetFeaturesConfiguration();
            features.IsMultiTenancyEnabled = true;
            await Repository.FeaturesConfiguration.ModifyFeaturesConfiguration(features);
            await Repository.Client.RefreshRootDocument();
        }

        private static EnvironmentResource[] GetEnvironmentsForCustomer(EnvironmentResource[] environments, TenantResource tenant)
        {
            return environments.Where(e => e.Name == "MT Production")
                .Concat(environments.Where(e => tenant.IsVIP() && e.Name == "MT Staging"))
                .Concat(environments.Where(e => tenant.IsBetaTester() && e.Name == "MT Beta"))
                .ToArray();
        }

        void TagCustomerByConvention(TenantResource customer, TagResource[] allTags)
        {
            var getTag = new Func<string, TagResource>(name => allTags.Single(t => t.Name == name));
            var sharedTags = allTags.Where(t => t.Name.StartsWith("Shared"));

            customer.WithTag(getTag("External"));
            customer.WithTag(customer.IsVIP() ? getTag("VIP") : customer.IsTrial() ? getTag("Trial") : getTag("Standard"));
            customer.WithTag(customer.IsVIP() ? getTag("Dedicated") : sharedTags.GetRandom());
            customer.WithTag(customer.IsEarlyAdopter() ? getTag("Early adopter") : getTag("Stable"));
            if (customer.IsBetaTester()) customer.WithTag(getTag("2.x Beta"));
        }

        private async Task FillOutTenantVariablesByConvention(
            TenantEditor tenantEditor,
            ProjectResource[] projects,
            EnvironmentResource[] environments,
            LibraryVariableSetResource[] libraryVariableSets)
        {
            var tenant = tenantEditor.Instance;
            var projectLookup = projects.ToDictionary(p => p.Id);
            var libraryVariableSetLookup = libraryVariableSets.ToDictionary(l => l.Id);
            var environmentLookup = environments.ToDictionary(e => e.Id);

            var tenantVariables = (await tenantEditor.Variables).Instance;

            // Library variables
            foreach (var libraryVariable in tenantVariables.LibraryVariables)
            {
                foreach (var template in libraryVariableSetLookup[libraryVariable.Value.LibraryVariableSetId].Templates)
                {
                    var value = TryFillLibraryVariableByConvention(template, tenant);
                    if (value != null)
                    {
                        libraryVariable.Value.Variables[template.Id] = value;
                    }
                }
            }

            // Project variables
            foreach (var projectVariable in tenantVariables.ProjectVariables)
            {
                foreach (var template in projectLookup[projectVariable.Value.ProjectId].Templates)
                {
                    foreach (var connectedEnvironmentId in tenant.ProjectEnvironments[projectVariable.Value.ProjectId])
                    {
                        var environment = environmentLookup[connectedEnvironmentId];
                        var value = TryFillProjectVariableByConvention(template, tenant, environment);
                        if (value != null)
                        {
                            projectVariable.Value.Variables[connectedEnvironmentId][template.Id] = value;
                        }
                    }
                }
            }
        }

        private PropertyValueResource TryFillLibraryVariableByConvention(ActionTemplateParameterResource template, TenantResource tenant)
        {
            if (template.Name == VariableKeys.StandardTenantDetails.TenantAlias) return new PropertyValueResource(tenant.Name.Replace(" ", "-").ToLowerInvariant());
            if (template.Name == VariableKeys.StandardTenantDetails.TenantRegion) return new PropertyValueResource(Region.All.GetRandom().Alias);
            if (template.Name == VariableKeys.StandardTenantDetails.TenantContactEmail) return new PropertyValueResource(tenant.Name.Replace(" ", ".").ToLowerInvariant() + "@test.com");

            return null;
        }

        private PropertyValueResource TryFillProjectVariableByConvention(ActionTemplateParameterResource template, TenantResource tenant, EnvironmentResource environment)
        {
            if (template.Name == VariableKeys.ProjectTenantVariables.TenantDatabasePassword) return new PropertyValueResource(RandomStringGenerator.Generate(16), isSensitive: true);

            return null;
        }

        private IEnumerable<TenantSampleData> GetSampleTenantData(string seed, int numberOfTenants)
        {
            dynamic randomTenantData;
            using (var client = new WebClient())
            {
                var json = client.DownloadString($"http://api.randomuser.me/?seed={seed}&nat=AU&results={numberOfTenants}");
                randomTenantData = JsonConvert.DeserializeObject(json);
            }

            foreach (var random in randomTenantData.results)
            {
                yield return new TenantSampleData($"{random.name.first} {random.name.last}".Titleize(), (string)random.picture.medium);
            }
        }

        class TenantSampleData
        {
            public TenantSampleData(string name, string logoUrl)
            {
                Name = name;
                LogoUrl = logoUrl;
            }

            public string Name { get; }
            public string LogoUrl { get; }
        }
    }
}