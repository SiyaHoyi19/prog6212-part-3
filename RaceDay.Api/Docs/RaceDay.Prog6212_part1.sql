USE RaceDay_script;
DROP TABLE IF EXISTS Results;
DROP TABLE IF EXISTS EventEnrolments;
DROP TABLE IF EXISTS Categories;
DROP TABLE IF EXISTS Events;
DROP TABLE IF EXISTS Authentication;
DROP TABLE IF EXISTS UserProfile;

--Creates Tables 
CREATE TABLE UserProfile(
user_id int primary key,
fullname varchar(50) NOT NULL,
email varchar(50) NOT NULL,
phonenumber varchar(15) NOT NULL,
dateofbirth date NOT NULL,
);

CREATE TABLE Authentication(
authentication_id int primary key,
user_id int,
username varchar(50) NOT NULL,
role varchar(50) NOT NULL DEFAULT 'Organiser',
passwordhash varchar(100) NOT NULL,
--Foreign Constraints
foreign key (user_id) REFERENCES UserProfile(user_id)
);

CREATE TABLE Events(
event_id int primary key,
organiser_id int NOT NULL,
name varchar(50) NOT NULL,
description varchar(50) NOT NULL,
date date NOT NULL,
location varchar(50) NOT NULL,
distance varchar(5) NOT NULL,
event_type varchar(50) NOT NULL,
--Foreign Constraints 
foreign key (organiser_id) REFERENCES UserProfile(user_id)
);

CREATE TABLE Categories(
category_id int primary key,
event_id int NOT NULL,
name varchar(50) NOT NULL,
distance varchar(50) NOT NULL,
minAge int NOT NULL,
--Foreign Constraints
foreign key (event_id) REFERENCES Events(event_id)
);

CREATE TABLE EventEnrolments(
enrolment_id int primary key,
user_id int NOT NULL,
event_id int NOT NULL,
category_id int NOT NULL,
date date DEFAULT GETDATE(),
--Foreign Constraints
foreign key (user_id) REFERENCES UserProfile(user_id),
foreign key (event_id) REFERENCES Events(event_id),
foreign key (category_id) REFERENCES Categories(category_id)
);

CREATE TABLE Results(
result_id int primary key,
finishtime int NULL,
position varchar(50) NULL,
resultstatus varchar(50) NOT NULL DEFAULT 'Completed',
);

-- 2 organisers and 2 participants)
INSERT INTO UserProfile(user_id, fullname, email, phonenumber, dateofbirth)
VALUES
(1, 'Siyahluma Hoyi', 'siyahlumahoyi21@gmail.com', '0682544332', '2006-07-17'),
(2, 'Vuyo Hoyi', 'Vuyoshoyi@gmail.com', '0782987765', '1976-04-27'),
(3, 'Musa Badi', 'Musabadi@gmail.com', '0831467791', '2001-11-21'),
(4, 'Imange Ngutyana', 'Imangengutyana@gmail.com', '0731914569', '1997-04-04');

-- 1 and 2 are organisers, 3 and 4 are particicants
INSERT INTO Authentication(authentication_id, user_id, username, passwordhash, role)
VALUES
(1, 1, 'siyahluma', 'hashedpassword1', 'organiser'),
(2, 2, 'vuyo', 'hashedpassword2', 'organiser'),
(3, 3, 'musa', 'hashedpassword3', 'participant'),
(4, 4, 'imange', 'hashedpassword4', 'participant');
 
-- 3 events
INSERT INTO Events(event_id, organiser_id, name,description,date, location, distance, event_type)
VALUES
(1, 1, 'road running', 'City Marathon', '2026-09-12', 'Pieternaritzburg', '5km', 'Fun race'),
(2, 1, 'walking', 'Active Mobility', '2026-10-03', 'Durban', '10km', 'Group walks'),
(3, 2, 'cycling community', 'Inclusivity', '2026-11-15', 'Cape Town', '21km', 'Social Group Rides');

-- Categories(categories for each event)
INSERT INTO Categories(category_id, event_id, name, distance, minAge)
VALUES
(1, 1, '5km road running', '5km', 10),
(2, 1, '10km road running', '10km', 16),
(3, 2, '10km walking', '10km', 16),
(4, 2, 'walking', '21km', 18),
(5, 3, 'cycling community', '21km', 18),
(6, 3, 'cycling community', '42km', 18);

-- Event Enrolments(sample enrolments)
INSERT INTO EventEnrolments(enrolment_id, user_id, event_id, category_id, date)
VALUES
(1, 3, 1, 2, '2026-08-20'),
(2, 4, 1, 1, '2026-08-21'),  
(3, 3, 2, 4, '2026-08-25'),  
(4, 4, 3, 6, '2026-08-27'); 

-- Results
INSERT INTO Results(result_id, finishtime, position, resultstatus)
VALUES
(1, 2745, '3', 'Completed'),
(2, 1890, '1', 'Completed'),
(3, 5820, '12', 'Completed'),
(4, NULL, NULL, 'DNF');

SELECT * FROM UserProfile;
SELECT * FROM Authentication;
SELECT * FROM Events;
SELECT * FROM Categories;
SELECT * FROM EventEnrolments;
SELECT * FROM Results;