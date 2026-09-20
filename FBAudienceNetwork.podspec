Pod::Spec.new do |s|
  s.name             = 'FBAudienceNetwork'
  s.version          = '6.21.1'
  s.summary          = 'Facebook Audience Network SDK'
  s.homepage         = 'https://developers.facebook.com/docs/audience-network'
  s.license          = { :type => 'Commercial' }
  s.author           = 'Meta'
  s.platform         = :ios, '12.0'
  s.source           = { :path => '.' }

  s.default_subspec = 'Static'

  s.subspec 'Static' do |ss|
    ss.vendored_frameworks = 'Static/FBAudienceNetwork.xcframework'
    ss.frameworks       = 'AVFoundation', 'CoreGraphics', 'CoreMedia', 'CoreTelephony', 'StoreKit', 'UIKit'
    ss.weak_frameworks  = 'AdSupport', 'AppTrackingTransparency', 'SafariServices', 'WebKit'
    ss.libraries        = 'c++', 'xml2', 'z'
  end

  s.subspec 'Dynamic' do |ss|
    ss.vendored_frameworks = 'Dynamic/FBAudienceNetwork.xcframework'
    ss.frameworks       = 'AVFoundation', 'CoreGraphics', 'CoreMedia', 'CoreTelephony', 'StoreKit', 'UIKit'
    ss.weak_frameworks  = 'AdSupport', 'AppTrackingTransparency', 'SafariServices', 'WebKit'
    ss.libraries        = 'c++', 'xml2', 'z'
  end
end