IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TeacherProfiles')
BEGIN
    CREATE TABLE TeacherProfiles (
        TeacherProfileId INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL UNIQUE,
        DateOfBirth DATE NULL,
        HighestDegree NVARCHAR(150) NULL,
        Institution NVARCHAR(150) NULL,
        YearOfGraduation INT NULL,
        SubjectSpecialization NVARCHAR(150) NULL,
        YearsOfExperience INT NULL,
        Bio NVARCHAR(1000) NULL,
        PhotoPath NVARCHAR(255) NULL,
        IsVerified BIT NOT NULL DEFAULT 0,
        Rating DECIMAL(3,2) NOT NULL DEFAULT 5.0,
        CONSTRAINT FK_TeacherProfiles_Users FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
    );
END

-- Seed default profile for teacher UserId = 1 if not exists
IF EXISTS (SELECT * FROM Users WHERE UserId = 1 AND Role = 'Teacher')
BEGIN
    IF NOT EXISTS (SELECT * FROM TeacherProfiles WHERE UserId = 1)
    BEGIN
        INSERT INTO TeacherProfiles (UserId, DateOfBirth, HighestDegree, Institution, YearOfGraduation, SubjectSpecialization, YearsOfExperience, Bio, PhotoPath, IsVerified, Rating)
        VALUES (1, '1990-05-15', 'Master of Science in Applied Mathematics', 'Tribhuvan University', 2012, 'Calculus & Statistics', 10, 'I am a passionate Mathematics educator with over 10 years of experience in higher secondary education. My goal is to make complex mathematical concepts accessible and engaging for students.', NULL, 1, 4.9);
    END
END
