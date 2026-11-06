import XCTest
@testable import Passi

final class ProjectSmokeTests: XCTestCase {
    func testApplicationName() {
        XCTAssertEqual(AppMetadata.name, "Passi")
    }
}
